using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly ICatalogDbContext _db;
    private readonly IProductContentSanitizer _sanitizer;

    public ProductService(ICatalogDbContext db, IProductContentSanitizer sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<PagedResult<ProductResponse>> ListAsync(PagedRequest request, Guid? categoryId, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (categoryId is { } category)
        {
            query = query.Where(p => p.CategoryId == category);
        }

        if (isActive is { } active)
        {
            query = query.Where(p => p.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Slug, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);

        // Projected so the rich-text Content column is never read for a list (only the editor needs it).
        // Links come from a split query: a single JOIN would multiply every product row by
        // (images × modifier groups) before paging.
        return await query
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Slug,
                p.CategoryId,
                p.Price,
                p.OldPrice,
                p.Description,
                null,
                p.IsActive,
                p.Badge,
                p.Rating,
                p.RatingCount,
                p.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId).ToList(),
                p.ModifierGroups.OrderBy(g => g.SortOrder).Select(g => g.ModifierGroupId).ToList(),
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .AsSplitQuery()
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureReferencesExistAsync(request.CategoryId, request.ModifierGroupIds, cancellationToken);
        var slug = await ResolveSlugAsync(request.Slug, request.Name, productId: null, cancellationToken);

        var product = Product.Create(request.Name, slug, request.CategoryId, ToDetails(request.Price, request.OldPrice,
            request.Description, request.Content, request.IsActive, request.Badge, request.Rating, request.RatingCount));
        product.ReplaceMedia(request.MediaIds ?? []);
        product.ReplaceModifierGroups(request.ModifierGroupIds ?? []);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await FindOrThrowAsync(id, cancellationToken);
        await EnsureReferencesExistAsync(request.CategoryId, request.ModifierGroupIds, cancellationToken);
        // A blank slug keeps the current one: renaming a product must not silently change its public URL.
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? product.Slug
            : await ResolveSlugAsync(request.Slug, request.Name, product.Id, cancellationToken);

        // Content null = keep the current detail: list reads never carry Content, so a caller that edits
        // from list data (e.g. CSV import) must not wipe it. An empty string clears it.
        var content = request.Content is null ? product.Content : request.Content;
        product.Update(request.Name, slug, request.CategoryId, ToDetails(request.Price, request.OldPrice,
            request.Description, content, request.IsActive, request.Badge, request.Rating, request.RatingCount));
        product.ReplaceMedia(request.MediaIds ?? []);
        product.ReplaceModifierGroups(request.ModifierGroupIds ?? []);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    /// <summary>Hard delete. Image/modifier links and the product's places on sales menus go with it
    /// (cascade); past orders keep their own snapshot of the product and are unaffected.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await FindOrThrowAsync(id, cancellationToken);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureReferencesExistAsync(Guid categoryId, IReadOnlyList<Guid>? modifierGroupIds, CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new BusinessRuleValidationException("The category does not exist.");
        }

        var requestedGroupIds = (modifierGroupIds ?? []).Distinct().ToList();
        if (requestedGroupIds.Count == 0)
        {
            return;
        }

        var existingCount = await _db.ModifierGroups.CountAsync(g => requestedGroupIds.Contains(g.Id), cancellationToken);
        if (existingCount != requestedGroupIds.Count)
        {
            throw new BusinessRuleValidationException("One or more modifier groups do not exist.");
        }
    }

    /// <summary>An explicit slug must be free (409 otherwise); a generated one gets a numeric suffix until
    /// it is unique ("banh-cuon", "banh-cuon-2", ...).</summary>
    private async Task<string> ResolveSlugAsync(string? requestedSlug, string name, Guid? productId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedSlug))
        {
            var slug = requestedSlug.Trim();
            if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != productId, cancellationToken))
            {
                throw new ConflictException($"Another product already uses the slug '{slug}'.");
            }

            return slug;
        }

        var baseSlug = Slug.FromText(name);
        if (baseSlug.Length == 0)
        {
            throw new BusinessRuleValidationException("A slug cannot be generated from this name; provide one explicitly.");
        }

        var takenSlugs = await _db.Products
            .Where(p => p.Id != productId && (p.Slug == baseSlug || p.Slug.StartsWith(baseSlug + "-")))
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken);

        return UniqueSlug(baseSlug, takenSlugs.ToHashSet());
    }

    private static string UniqueSlug(string baseSlug, IReadOnlySet<string> takenSlugs)
    {
        if (!takenSlugs.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!takenSlugs.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private ProductDetails ToDetails(decimal price, decimal? oldPrice, string? description, string? content, bool isActive,
        string? badge, decimal? rating, int? ratingCount) =>
        new(price, oldPrice, description, string.IsNullOrWhiteSpace(content) ? null : _sanitizer.Sanitize(content),
            isActive, badge, rating, ratingCount);

    private async Task<Product> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Products
            .Include(p => p.Media)
            .Include(p => p.ModifierGroups)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.CategoryId,
        product.Price,
        product.OldPrice,
        product.Description,
        product.Content,
        product.IsActive,
        product.Badge,
        product.Rating,
        product.RatingCount,
        product.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId).ToList(),
        product.ModifierGroups.OrderBy(g => g.SortOrder).Select(g => g.ModifierGroupId).ToList(),
        product.CreatedAtUtc,
        product.UpdatedAtUtc);
}

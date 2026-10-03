using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

public sealed class PromotionService : IPromotionService
{
    private readonly ICatalogDbContext _db;

    public PromotionService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PromotionResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken)
    {
        var query = _db.Promotions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!PromotionWireFormat.TryParseStatus(status, out var parsedStatus))
            {
                throw new BusinessRuleValidationException("Status must be draft, active or inactive.");
            }

            query = query.Where(p => p.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, pattern) || EF.Functions.ILike(p.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc);

        // Scope links are loaded with a split query: a single JOIN would multiply every promotion row by
        // (products × categories) before paging.
        var page = await query
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .AsSplitQuery()
            .ToPagedResultAsync(request, cancellationToken);

        return new PagedResult<PromotionResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PromotionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<PromotionResponse> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var details = ToDetails(request.Code, request.Name, request.Description, request.Type, request.Value, request.MaxDiscountAmount,
            request.MinimumOrderAmount, request.StartAt, request.EndAt, request.UsageLimit, request.Status);
        var (productIds, categoryIds) = ResolveScope(details.Type, request.ApplicableProductIds, request.ApplicableCategoryIds);

        await EnsureCodeIsFreeAsync(Promotion.NormalizeCode(details.Code), promotionId: null, cancellationToken);
        await EnsureScopeExistsAsync(productIds, categoryIds, cancellationToken);

        var promotion = Promotion.Create(details);
        promotion.ReplaceProducts(productIds);
        promotion.ReplaceCategories(categoryIds);

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(promotion);
    }

    public async Task<PromotionResponse> UpdateAsync(Guid id, UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = await FindOrThrowAsync(id, cancellationToken);

        var details = ToDetails(request.Code, request.Name, request.Description, request.Type, request.Value, request.MaxDiscountAmount,
            request.MinimumOrderAmount, request.StartAt, request.EndAt, request.UsageLimit, request.Status);
        var (productIds, categoryIds) = ResolveScope(details.Type, request.ApplicableProductIds, request.ApplicableCategoryIds);

        await EnsureCodeIsFreeAsync(Promotion.NormalizeCode(details.Code), promotion.Id, cancellationToken);
        await EnsureScopeExistsAsync(productIds, categoryIds, cancellationToken);

        promotion.Update(details);
        promotion.ReplaceProducts(productIds);
        promotion.ReplaceCategories(categoryIds);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(promotion);
    }

    /// <summary>Hard delete; the product/category scope links go with it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await FindOrThrowAsync(id, cancellationToken);
        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Promotion> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Promotions
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), id);

    private async Task EnsureCodeIsFreeAsync(string normalizedCode, Guid? promotionId, CancellationToken cancellationToken)
    {
        if (await _db.Promotions.AnyAsync(p => p.Code == normalizedCode && p.Id != promotionId, cancellationToken))
        {
            throw new ConflictException($"A promotion with code '{normalizedCode}' already exists.");
        }
    }

    /// <summary>Only a product discount has a scope (and then it must not be empty); for every other type
    /// the lists are ignored, so switching a promotion's type cleans up its old scope.</summary>
    private static (List<Guid> ProductIds, List<Guid> CategoryIds) ResolveScope(
        PromotionType type, IReadOnlyList<Guid>? productIds, IReadOnlyList<Guid>? categoryIds)
    {
        if (type != PromotionType.ProductDiscount)
        {
            return ([], []);
        }

        var products = (productIds ?? []).Distinct().ToList();
        var categories = (categoryIds ?? []).Distinct().ToList();
        if (products.Count == 0 && categories.Count == 0)
        {
            throw new BusinessRuleValidationException("A product-discount promotion needs at least one product or category.");
        }

        return (products, categories);
    }

    private async Task EnsureScopeExistsAsync(List<Guid> productIds, List<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (productIds.Count > 0 && await _db.Products.CountAsync(p => productIds.Contains(p.Id), cancellationToken) != productIds.Count)
        {
            throw new BusinessRuleValidationException("One or more products do not exist.");
        }

        if (categoryIds.Count > 0 && await _db.Categories.CountAsync(c => categoryIds.Contains(c.Id), cancellationToken) != categoryIds.Count)
        {
            throw new BusinessRuleValidationException("One or more categories do not exist.");
        }
    }

    private static PromotionDetails ToDetails(
        string code, string name, string? description, string type, decimal value, decimal? maxDiscountAmount,
        decimal? minimumOrderAmount, DateTime? startAt, DateTime? endAt, int? usageLimit, string status)
    {
        // The validators already checked both names; a failed parse here would be a programming error.
        PromotionWireFormat.TryParseType(type, out var parsedType);
        PromotionWireFormat.TryParseStatus(status, out var parsedStatus);

        return new PromotionDetails(code, name, description, parsedType, value, maxDiscountAmount, minimumOrderAmount,
            startAt, endAt, usageLimit, parsedStatus);
    }

    private static PromotionResponse ToResponse(Promotion promotion) =>
        new(promotion.Id, promotion.Code, promotion.Name, promotion.Description, PromotionWireFormat.ToWire(promotion.Type),
            promotion.Value, promotion.MaxDiscountAmount, promotion.MinimumOrderAmount, promotion.StartAtUtc, promotion.EndAtUtc,
            promotion.UsageLimit, promotion.UsageCount,
            promotion.Products.Select(link => link.ProductId).ToList(),
            promotion.Categories.Select(link => link.CategoryId).ToList(),
            PromotionWireFormat.ToWire(promotion.Status), promotion.CreatedAtUtc, promotion.UpdatedAtUtc);
}

using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly ICatalogDbContext _db;

    public CategoryService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CategoryResponse>> ListAsync(PagedRequest request, Guid? parentId, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();

        if (parentId is { } parent)
        {
            query = query.Where(c => c.ParentId == parent);
        }

        if (isActive is { } active)
        {
            query = query.Where(c => c.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern));
        }

        query = request.IsDescending
            ? query.OrderByDescending(c => c.SortOrder).ThenByDescending(c => c.Name)
            : query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name);

        var projected = query.Select(c => new CategoryResponse(c.Id, c.Name, c.ParentId, c.SortOrder, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var parentById = await LoadParentLinksAsync(cancellationToken);
        EnsureParentFits(request.ParentId, subtreeHeight: 1, parentById);

        var category = Category.Create(request.Name, request.ParentId, request.SortOrder, request.IsActive);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);

        var parentById = await LoadParentLinksAsync(cancellationToken);
        if (request.ParentId is { } parentId && parentId != id && IsDescendant(parentId, id, parentById))
        {
            throw new BusinessRuleValidationException("A category cannot be moved under one of its own sub-categories.");
        }

        if (request.ParentId != id)
        {
            EnsureParentFits(request.ParentId, SubtreeHeight(id, parentById), parentById);
        }

        category.Update(request.Name, request.ParentId, request.SortOrder, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);

        if (await _db.Categories.AnyAsync(c => c.ParentId == id, cancellationToken))
        {
            throw new ConflictException("The category has sub-categories. Move or delete them first.");
        }

        if (await _db.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            throw new ConflictException("The category still has products. Move or delete them first.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The whole tree as child → parent links: the catalog taxonomy is small (tens of rows), so
    /// depth/cycle checks walk it in memory instead of issuing one query per level.</summary>
    private async Task<Dictionary<Guid, Guid?>> LoadParentLinksAsync(CancellationToken cancellationToken) =>
        await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.ParentId, cancellationToken);

    /// <summary>Placing a subtree of the given height under the parent must keep the tree within
    /// <see cref="Category.MaxDepth"/> levels.</summary>
    private static void EnsureParentFits(Guid? parentId, int subtreeHeight, Dictionary<Guid, Guid?> parentById)
    {
        if (parentId is not { } parent)
        {
            return;
        }

        if (!parentById.ContainsKey(parent))
        {
            throw new BusinessRuleValidationException("The parent category does not exist.");
        }

        var parentDepth = 1;
        for (var current = parentById[parent]; current is { } ancestor; current = parentById.GetValueOrDefault(ancestor))
        {
            parentDepth++;
        }

        if (parentDepth + subtreeHeight > Category.MaxDepth)
        {
            throw new BusinessRuleValidationException($"Categories can be nested at most {Category.MaxDepth} levels deep.");
        }
    }

    private static bool IsDescendant(Guid candidateId, Guid ancestorId, Dictionary<Guid, Guid?> parentById)
    {
        for (var current = parentById.GetValueOrDefault(candidateId); current is { } parent; current = parentById.GetValueOrDefault(parent))
        {
            if (parent == ancestorId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Number of levels in the subtree rooted at the category, itself included (a leaf is 1).</summary>
    private static int SubtreeHeight(Guid rootId, Dictionary<Guid, Guid?> parentById)
    {
        var children = parentById.Where(link => link.Value == rootId).Select(link => link.Key).ToList();
        return 1 + (children.Count == 0 ? 0 : children.Max(child => SubtreeHeight(child, parentById)));
    }

    private async Task<Category> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

    private static CategoryResponse ToResponse(Category category) => new(
        category.Id,
        category.Name,
        category.ParentId,
        category.SortOrder,
        category.IsActive,
        category.CreatedAtUtc,
        category.UpdatedAtUtc);
}

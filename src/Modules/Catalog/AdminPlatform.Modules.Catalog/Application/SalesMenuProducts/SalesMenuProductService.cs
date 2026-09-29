using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;

public sealed class SalesMenuProductService : ISalesMenuProductService
{
    private readonly ICatalogDbContext _db;

    public SalesMenuProductService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SalesMenuProductResponse>> ListAsync(PagedRequest request, Guid? salesMenuId, Guid? productId, CancellationToken cancellationToken)
    {
        var query = _db.SalesMenuProducts.AsNoTracking().AsQueryable();

        if (salesMenuId is { } menuId)
        {
            query = query.Where(mp => mp.SalesMenuId == menuId);
        }

        if (productId is { } product)
        {
            query = query.Where(mp => mp.ProductId == product);
        }

        query = request.IsDescending
            ? query.OrderBy(mp => mp.SalesMenuId).ThenByDescending(mp => mp.SortOrder)
            : query.OrderBy(mp => mp.SalesMenuId).ThenBy(mp => mp.SortOrder);

        var projected = query.Select(mp => new SalesMenuProductResponse(
            mp.Id, mp.SalesMenuId, mp.ProductId, mp.PriceOverride, mp.SortOrder, mp.IsAvailable, mp.CreatedAtUtc, mp.UpdatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<SalesMenuProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<SalesMenuProductResponse> CreateAsync(CreateSalesMenuProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureValidPlacementAsync(request.SalesMenuId, request.ProductId, currentId: null, cancellationToken);

        var menuProduct = SalesMenuProduct.Create(request.SalesMenuId, request.ProductId, request.PriceOverride, request.SortOrder, request.IsAvailable);
        _db.SalesMenuProducts.Add(menuProduct);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menuProduct);
    }

    public async Task<SalesMenuProductResponse> UpdateAsync(Guid id, UpdateSalesMenuProductRequest request, CancellationToken cancellationToken)
    {
        var menuProduct = await FindOrThrowAsync(id, cancellationToken);
        await EnsureValidPlacementAsync(request.SalesMenuId, request.ProductId, id, cancellationToken);

        menuProduct.Update(request.SalesMenuId, request.ProductId, request.PriceOverride, request.SortOrder, request.IsAvailable);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menuProduct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var menuProduct = await FindOrThrowAsync(id, cancellationToken);
        _db.SalesMenuProducts.Remove(menuProduct);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureValidPlacementAsync(Guid salesMenuId, Guid productId, Guid? currentId, CancellationToken cancellationToken)
    {
        if (!await _db.SalesMenus.AnyAsync(m => m.Id == salesMenuId, cancellationToken))
        {
            throw new BusinessRuleValidationException("The menu does not exist.");
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            throw new BusinessRuleValidationException("The product does not exist.");
        }

        var alreadyPlaced = await _db.SalesMenuProducts.AnyAsync(
            mp => mp.SalesMenuId == salesMenuId && mp.ProductId == productId && mp.Id != currentId, cancellationToken);
        if (alreadyPlaced)
        {
            throw new ConflictException("The product is already on this menu.");
        }
    }

    private async Task<SalesMenuProduct> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.SalesMenuProducts.SingleOrDefaultAsync(mp => mp.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesMenuProduct), id);

    private static SalesMenuProductResponse ToResponse(SalesMenuProduct mp) => new(
        mp.Id, mp.SalesMenuId, mp.ProductId, mp.PriceOverride, mp.SortOrder, mp.IsAvailable, mp.CreatedAtUtc, mp.UpdatedAtUtc);
}

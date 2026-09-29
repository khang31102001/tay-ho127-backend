using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.PublicCatalog;

/// <summary>Unpaged on purpose: a restaurant catalog is bounded (tens of categories, low hundreds of
/// products) and the website needs all of it to build menus, product pages and price checks. Revisit with
/// per-menu endpoints if the catalog ever grows into the thousands.</summary>
public sealed class PublicCatalogService : IPublicCatalogService
{
    private readonly ICatalogDbContext _db;

    public PublicCatalogService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PublicCatalogResponse> GetAsync(CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new PublicCategoryResponse(c.Id, c.Name, c.ParentId, c.SortOrder))
            .ToListAsync(cancellationToken);

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Media)
            .Include(p => p.ModifierGroups)
            .AsSplitQuery()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var salesMenus = await _db.SalesMenus.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new PublicSalesMenuResponse(m.Id, m.Code, m.Name))
            .ToListAsync(cancellationToken);

        var salesMenuProducts = await _db.SalesMenuProducts.AsNoTracking()
            .Where(mp => mp.IsAvailable
                && _db.SalesMenus.Any(m => m.Id == mp.SalesMenuId && m.IsActive)
                && _db.Products.Any(p => p.Id == mp.ProductId && p.IsActive))
            .OrderBy(mp => mp.SalesMenuId).ThenBy(mp => mp.SortOrder)
            .Select(mp => new PublicSalesMenuProductResponse(mp.Id, mp.SalesMenuId, mp.ProductId, mp.PriceOverride, mp.SortOrder))
            .ToListAsync(cancellationToken);

        var modifierGroups = await _db.ModifierGroups.AsNoTracking()
            .Include(g => g.Options)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return new PublicCatalogResponse(
            categories,
            products.Select(p => new PublicProductResponse(
                p.Id,
                p.Slug,
                p.Name,
                p.CategoryId,
                p.Price,
                p.OldPrice,
                p.Description,
                p.Badge,
                p.Rating,
                p.RatingCount,
                p.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId).ToList(),
                p.ModifierGroups.OrderBy(g => g.SortOrder).Select(g => g.ModifierGroupId).ToList())).ToList(),
            salesMenus,
            salesMenuProducts,
            modifierGroups.Select(g => new PublicModifierGroupResponse(
                g.Id,
                g.Name,
                g.SelectionType.ToString().ToLowerInvariant(),
                g.IsRequired,
                g.Options
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new PublicModifierOptionResponse(o.Id, o.Label, o.PriceAdjustment, o.IsDefault))
                    .ToList())).ToList());
    }
}

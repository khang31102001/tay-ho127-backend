using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Pricing;

public sealed record ModifierOptionPricing(Guid Id, string Label, decimal PriceAdjustment);

public sealed record ModifierGroupPricing(Guid Id, string Name, bool IsMultiple, IReadOnlyList<ModifierOptionPricing> Options);

/// <summary>What an order needs to price one dish: its current price, whether it is sellable, its first image and the
/// modifier groups (with options and surcharges) that apply to it.</summary>
public sealed record ProductPricing(
    Guid Id,
    string Name,
    decimal Price,
    bool IsActive,
    string? PrimaryMediaId,
    IReadOnlyList<ModifierGroupPricing> ModifierGroups);

/// <summary>The Catalog's read contract for ordering (used by the Sales module through a Host adapter): the
/// authoritative current prices and modifiers of the requested products.</summary>
public interface ICatalogPricingQueryService
{
    /// <summary>Only products that exist are returned (inactive ones included, flagged).</summary>
    Task<IReadOnlyList<ProductPricing>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}

public sealed class CatalogPricingQueryService : ICatalogPricingQueryService
{
    private readonly ICatalogDbContext _db;

    public CatalogPricingQueryService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductPricing>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Media)
            .Include(p => p.ModifierGroups)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var groupIds = products.SelectMany(p => p.ModifierGroups.Select(g => g.ModifierGroupId)).Distinct().ToList();
        var groups = groupIds.Count == 0
            ? []
            : await _db.ModifierGroups.AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .Include(g => g.Options)
                .AsSplitQuery()
                .ToDictionaryAsync(g => g.Id, cancellationToken);

        return products.Select(p => new ProductPricing(
            p.Id,
            p.Name,
            p.Price,
            p.IsActive,
            p.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId).FirstOrDefault(),
            p.ModifierGroups
                .OrderBy(link => link.SortOrder)
                .Select(link => groups.GetValueOrDefault(link.ModifierGroupId))
                .OfType<ModifierGroup>()
                .Select(g => new ModifierGroupPricing(
                    g.Id,
                    g.Name,
                    g.SelectionType == ModifierSelectionType.Multiple,
                    g.Options.OrderBy(o => o.SortOrder).Select(o => new ModifierOptionPricing(o.Id, o.Label, o.PriceAdjustment)).ToList()))
                .ToList())).ToList();
    }
}

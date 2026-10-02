using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A product placed on a sales menu, with menu-specific price, position and availability.
/// A product appears at most once per menu.</summary>
public sealed class SalesMenuProduct : AuditableEntity
{
    public Guid SalesMenuId { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>Menu-specific price; null means the product's own price applies.</summary>
    public decimal? PriceOverride { get; private set; }

    public int SortOrder { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    private SalesMenuProduct()
    {
        // EF Core
    }

    public static SalesMenuProduct Create(Guid salesMenuId, Guid productId, decimal? priceOverride, int sortOrder, bool isAvailable)
    {
        var menuProduct = new SalesMenuProduct { Id = Guid.NewGuid() };
        menuProduct.Update(salesMenuId, productId, priceOverride, sortOrder, isAvailable);
        return menuProduct;
    }

    public void Update(Guid salesMenuId, Guid productId, decimal? priceOverride, int sortOrder, bool isAvailable)
    {
        if (priceOverride < 0)
        {
            throw new BusinessRuleValidationException("Price override cannot be negative.");
        }

        SalesMenuId = Guard.NotEmpty(salesMenuId, nameof(salesMenuId));
        ProductId = Guard.NotEmpty(productId, nameof(productId));
        PriceOverride = priceOverride;
        SortOrder = sortOrder;
        IsAvailable = isAvailable;
    }
}

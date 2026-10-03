namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A product a <see cref="PromotionType.ProductDiscount"/> promotion applies to.</summary>
public sealed class PromotionProduct
{
    public Guid PromotionId { get; private set; }
    public Guid ProductId { get; private set; }

    private PromotionProduct()
    {
        // EF Core
    }

    internal static PromotionProduct Create(Guid promotionId, Guid productId) =>
        new() { PromotionId = promotionId, ProductId = productId };
}

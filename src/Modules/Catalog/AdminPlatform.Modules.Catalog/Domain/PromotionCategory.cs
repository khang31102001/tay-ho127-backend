namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A category a <see cref="PromotionType.ProductDiscount"/> promotion applies to. The category's
/// descendants are covered too (products hang off leaf categories of the 3-level tree).</summary>
public sealed class PromotionCategory
{
    public Guid PromotionId { get; private set; }
    public Guid CategoryId { get; private set; }

    private PromotionCategory()
    {
        // EF Core
    }

    internal static PromotionCategory Create(Guid promotionId, Guid categoryId) =>
        new() { PromotionId = promotionId, CategoryId = categoryId };
}

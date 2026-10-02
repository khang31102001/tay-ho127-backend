namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>Ordered link between a product and a modifier group that applies to it.</summary>
public sealed class ProductModifierGroup
{
    public Guid ProductId { get; private set; }
    public Guid ModifierGroupId { get; private set; }
    public int SortOrder { get; private set; }

    private ProductModifierGroup()
    {
        // EF Core
    }

    internal static ProductModifierGroup Create(Guid productId, Guid modifierGroupId, int sortOrder) =>
        new() { ProductId = productId, ModifierGroupId = modifierGroupId, SortOrder = sortOrder };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}

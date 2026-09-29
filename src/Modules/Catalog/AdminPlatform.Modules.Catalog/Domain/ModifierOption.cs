using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

public sealed class ModifierOption : Entity
{
    public Guid ModifierGroupId { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Added to the product price when picked; may be negative (a discount for the choice).</summary>
    public decimal PriceAdjustment { get; private set; }

    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }

    private ModifierOption()
    {
        // EF Core
    }

    internal static ModifierOption Create(Guid modifierGroupId, string label, decimal priceAdjustment, bool isDefault, int sortOrder)
    {
        var option = new ModifierOption { Id = Guid.NewGuid(), ModifierGroupId = modifierGroupId };
        option.Update(label, priceAdjustment, isDefault, sortOrder);
        return option;
    }

    internal void Update(string label, decimal priceAdjustment, bool isDefault, int sortOrder)
    {
        Label = Guard.NotNullOrWhiteSpace(label, nameof(label)).Trim();
        PriceAdjustment = priceAdjustment;
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }
}

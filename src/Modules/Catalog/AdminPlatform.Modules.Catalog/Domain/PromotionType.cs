namespace AdminPlatform.Modules.Catalog.Domain;

public enum PromotionType
{
    /// <summary>Value is a percentage (0–100] of the order subtotal.</summary>
    Percentage,

    /// <summary>Value is a fixed amount off the order subtotal.</summary>
    FixedAmount,

    /// <summary>Value is the percentage (0–100] of the shipping fee that is waived.</summary>
    FreeShipping,

    /// <summary>Value is a percentage (0–100] off only the cart lines of the promotion's products/categories.</summary>
    ProductDiscount,
}

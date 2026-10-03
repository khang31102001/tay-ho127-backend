using AdminPlatform.Modules.Catalog.Domain;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

/// <summary>Type: "percentage" | "fixed_amount" | "free_shipping" | "product_discount". Status: "draft" |
/// "active" | "inactive" (both case-insensitive). StartAt/EndAt are UTC. The applicable id lists are only
/// used (and required) for "product_discount"; for other types they are ignored.</summary>
public sealed record CreatePromotionRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? StartAt,
    DateTime? EndAt,
    int? UsageLimit,
    IReadOnlyList<Guid>? ApplicableProductIds,
    IReadOnlyList<Guid>? ApplicableCategoryIds,
    string Status);

/// <summary>Same fields as <see cref="CreatePromotionRequest"/>; the usage count is never editable here.</summary>
public sealed record UpdatePromotionRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? StartAt,
    DateTime? EndAt,
    int? UsageLimit,
    IReadOnlyList<Guid>? ApplicableProductIds,
    IReadOnlyList<Guid>? ApplicableCategoryIds,
    string Status);

/// <summary>Type and Status are returned in the same snake_case/lowercase wire format they are sent in.
/// Status is the stored value; an "active" promotion past its EndAt is expired (derived by the client).</summary>
public sealed record PromotionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? StartAt,
    DateTime? EndAt,
    int? UsageLimit,
    int UsageCount,
    IReadOnlyList<Guid> ApplicableProductIds,
    IReadOnlyList<Guid> ApplicableCategoryIds,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>One cart line. ProductId is text on purpose: a cart restored from before the catalog moved to
/// the Backend may hold legacy ids, which simply never match a promotion's scope.</summary>
public sealed record ValidatePromotionItem(string ProductId, decimal LineTotal);

public sealed record ValidatePromotionRequest(
    string Code,
    decimal Subtotal,
    decimal ShippingFee,
    IReadOnlyList<ValidatePromotionItem>? Items);

public sealed record PromotionSummary(Guid Id, string Code, string Name, string Type);

/// <summary>An unusable code is a normal outcome, not an error: it returns 200 with IsValid=false and a
/// Vietnamese Message the checkout shows as-is. Amounts are already computed server-side.</summary>
public sealed record ValidatePromotionResponse(
    bool IsValid,
    PromotionSummary? Promotion,
    decimal DiscountAmount,
    decimal ShippingDiscount,
    string? Message);

/// <summary>Maps the enums to/from the lowercase snake_case names used on the wire.</summary>
public static class PromotionWireFormat
{
    public static string ToWire(PromotionType type) => type switch
    {
        PromotionType.Percentage => "percentage",
        PromotionType.FixedAmount => "fixed_amount",
        PromotionType.FreeShipping => "free_shipping",
        PromotionType.ProductDiscount => "product_discount",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static bool TryParseType(string? value, out PromotionType type)
    {
        foreach (var candidate in Enum.GetValues<PromotionType>())
        {
            if (string.Equals(ToWire(candidate), value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }

    public static string ToWire(PromotionStatus status) => status.ToString().ToLowerInvariant();

    public static bool TryParseStatus(string? value, out PromotionStatus status) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out status) && Enum.IsDefined(status);
}

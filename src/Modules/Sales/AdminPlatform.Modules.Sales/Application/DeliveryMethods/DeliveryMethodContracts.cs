namespace AdminPlatform.Modules.Sales.Application.DeliveryMethods;

/// <summary>Code: lowercase letters, digits, '-' and '_' — unique, and never changes afterwards (orders and the
/// site refer to it). Type: "delivery" or "pickup" (a pickup method needs a PickupAddress). FreeShippingThreshold:
/// an order subtotal at or above it pays no BaseFee; null = never free. At most one method is the default —
/// saving one as default clears the flag on the others.</summary>
public sealed record CreateDeliveryMethodRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal BaseFee,
    decimal? FreeShippingThreshold,
    int? EstimatedMinMinutes,
    int? EstimatedMaxMinutes,
    string? PickupAddress,
    int DisplayOrder,
    bool IsActive,
    bool IsDefault);

/// <summary>Same fields as <see cref="CreateDeliveryMethodRequest"/> except the code, which is immutable.</summary>
public sealed record UpdateDeliveryMethodRequest(
    string Name,
    string? Description,
    string Type,
    decimal BaseFee,
    decimal? FreeShippingThreshold,
    int? EstimatedMinMinutes,
    int? EstimatedMaxMinutes,
    string? PickupAddress,
    int DisplayOrder,
    bool IsActive,
    bool IsDefault);

public sealed record DeliveryMethodResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal BaseFee,
    decimal? FreeShippingThreshold,
    int? EstimatedMinMinutes,
    int? EstimatedMaxMinutes,
    string? PickupAddress,
    int DisplayOrder,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>What the checkout needs — only active methods are ever returned.</summary>
public sealed record PublicDeliveryMethodResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal BaseFee,
    decimal? FreeShippingThreshold,
    int? EstimatedMinMinutes,
    int? EstimatedMaxMinutes,
    string? PickupAddress,
    int DisplayOrder,
    bool IsDefault);

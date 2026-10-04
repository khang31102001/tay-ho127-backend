namespace AdminPlatform.Modules.Sales.Application.Orders;

/// <summary>One picked option: a group and one of its options. Only ids travel — the server looks the label and
/// the price up itself.</summary>
public sealed record SelectedOptionRequest(Guid GroupId, Guid OptionId);

/// <summary>One dish line. Only ProductId, Quantity (1-99), an optional note and the picked modifiers are sent;
/// name, price and image are always taken from the catalog.</summary>
public sealed record OrderItemRequest(Guid ProductId, int Quantity, string? Note, IReadOnlyList<SelectedOptionRequest>? Modifiers);

/// <summary>Everything a customer decides when ordering. The server computes every amount: it never accepts a
/// price, a fee or a discount from the client — only a DiscountCode, which it re-evaluates against the real cart.
/// Phone: a Vietnamese number (0xxxxxxxxx or +84xxxxxxxxx). DeliveryAddress is ignored for a pickup method (the
/// method's own pickup address is used) and required otherwise. IdempotencyKey: generate one per checkout attempt
/// and resend it on retries — the same key returns the order that was already placed instead of a second one.</summary>
public sealed record CreateOrderRequest(
    string CustomerName,
    string Phone,
    string? Email,
    string? DeliveryAddress,
    string DeliveryMethodCode,
    string PaymentMethodCode,
    IReadOnlyList<OrderItemRequest> Items,
    bool WantsUtensils,
    string? Note,
    IReadOnlyList<SelectedOptionRequest>? OrderOptions,
    string? DiscountCode,
    string? IdempotencyKey);

/// <summary>Looking an order up as a guest takes BOTH the order code and the phone number it was placed with.</summary>
public sealed record OrderLookupRequest(string OrderCode, string Phone);

/// <summary>ToStatus: "confirmed" | "preparing" | "ready" | "delivering" | "completed" | "cancelled" — see the
/// order's NextStatuses for what is allowed now. Cancelling needs the extra orders.cancel permission.</summary>
public sealed record ChangeOrderStatusRequest(string ToStatus, string? Note);

public sealed record OrderModifierResponse(Guid GroupId, string GroupName, Guid OptionId, string OptionLabel, decimal PriceAdjustment);

/// <summary>ProductImage is the Media id of the dish's image (resolve it with the media API); UnitPrice is the
/// dish's own price, LineTotal already includes the modifiers and the quantity.</summary>
public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Note,
    IReadOnlyList<OrderModifierResponse> Modifiers);

public sealed record OrderStatusHistoryResponse(string? FromStatus, string ToStatus, DateTime ChangedAt, string ChangedBy, string? Note);

/// <summary>An order exactly as it was placed (a snapshot) plus its current state. NextStatuses lists the
/// statuses an admin may move it to now. PaymentId is the order's payment record (null only for data older than
/// the payments table).</summary>
public sealed record OrderResponse(
    Guid Id,
    string OrderCode,
    Guid? CustomerId,
    string CustomerName,
    string Phone,
    string? Email,
    string DeliveryAddressSnapshot,
    string PaymentMethodCode,
    string PaymentMethodLabel,
    string DeliveryMethodCode,
    string DeliveryMethodLabel,
    bool IsPickup,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderModifierResponse> OrderOptionSelections,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory,
    decimal Subtotal,
    decimal Discount,
    string? DiscountCode,
    Guid? PromotionId,
    decimal ShippingDiscount,
    decimal DeliveryFee,
    decimal TotalAmount,
    string OrderStatus,
    string PaymentStatus,
    bool WantsUtensils,
    string? Note,
    IReadOnlyList<string> NextStatuses,
    Guid? PaymentId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt);

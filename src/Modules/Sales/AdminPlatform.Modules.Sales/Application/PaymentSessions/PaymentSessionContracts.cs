using AdminPlatform.Modules.Sales.Application.Orders;

namespace AdminPlatform.Modules.Sales.Application.PaymentSessions;

/// <summary>Why a session was not paid (shown to the customer on their payment page).</summary>
public sealed record RejectPaymentSessionRequest(string? Note);

/// <summary>A dish line as quoted when the session was created (display only — confirming re-prices it).</summary>
public sealed record PaymentSessionLineResponse(
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Note,
    IReadOnlyList<OrderModifierResponse> Modifiers);

/// <summary>Status: "pending" | "success" | "failed" | "cancelled". Channel: "qr" | "digital_wallet". A pending session
/// is waiting for staff to confirm the money arrived; ReferenceCode is what the customer writes in the transfer.
/// OrderId/OrderCode are set once the session succeeded. The bank details are those of the payment method at
/// the time the session was created.</summary>
public sealed record PaymentSessionResponse(
    Guid Id,
    string ReferenceCode,
    string Status,
    string Channel,
    string PaymentMethodCode,
    string PaymentMethodLabel,
    string CustomerName,
    string Phone,
    string? Email,
    string DeliveryMethodCode,
    string DeliveryMethodLabel,
    bool IsPickup,
    string DeliveryAddressSnapshot,
    bool WantsUtensils,
    string? Note,
    IReadOnlyList<PaymentSessionLineResponse> Items,
    IReadOnlyList<OrderModifierResponse> OrderOptionSelections,
    decimal Subtotal,
    decimal ShippingFee,
    decimal Discount,
    string? DiscountCode,
    decimal ShippingDiscount,
    decimal TotalAmount,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountHolder,
    Guid? OrderId,
    string? OrderCode,
    string? ResolutionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime ExpiresAt);

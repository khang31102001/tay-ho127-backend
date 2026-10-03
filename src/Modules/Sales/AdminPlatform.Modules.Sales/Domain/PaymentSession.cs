using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

public enum PaymentSessionStatus
{
    Pending,
    Success,
    Failed,
    Cancelled,
}

/// <summary>Which way the customer pays up front — derived from the payment method's group, never sent by
/// the browser.</summary>
public enum PaymentChannel
{
    Qr,
    DigitalWallet,
}

/// <summary>A "reservation" of an order that is paid up front (QR transfer, wallet): the order does NOT exist
/// until someone with the payments permission confirms the money arrived. The session keeps what the customer
/// asked for (<see cref="RequestJson"/>) and the amounts that were quoted; confirming re-prices the request
/// and refuses to create the order if the price changed in the meantime. A session that is not confirmed in
/// time expires (cancelled) without ever creating an order.</summary>
public sealed class PaymentSession : AuditableEntity
{
    public const int MaxReferenceLength = 32;

    public string ReferenceCode { get; private set; } = string.Empty;
    public PaymentSessionStatus Status { get; private set; } = PaymentSessionStatus.Pending;
    public PaymentChannel Channel { get; private set; }
    public string PaymentMethodCode { get; private set; } = string.Empty;
    public string PaymentMethodLabel { get; private set; } = string.Empty;

    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }

    public string DeliveryMethodCode { get; private set; } = string.Empty;
    public string DeliveryMethodLabel { get; private set; } = string.Empty;
    public bool IsPickup { get; private set; }
    public string DeliveryAddressSnapshot { get; private set; } = string.Empty;
    public bool WantsUtensils { get; private set; }
    public string? CustomerNote { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal Discount { get; private set; }
    public string? DiscountCode { get; private set; }
    public Guid? PromotionId { get; private set; }
    public decimal ShippingDiscount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Bank details copied from the payment method at the time, so the transfer instructions the
    /// customer saw do not change under them.</summary>
    public string? BankName { get; private set; }

    public string? BankAccountNumber { get; private set; }
    public string? BankAccountHolder { get; private set; }

    public string? IdempotencyKey { get; private set; }

    /// <summary>The original order request (items, options, code) as JSON, replayed on confirmation.</summary>
    public string RequestJson { get; private set; } = string.Empty;

    /// <summary>Display lines (name, quantity, price) captured at quote time, as JSON.</summary>
    public string LinesJson { get; private set; } = "[]";

    public Guid? OrderId { get; private set; }
    public string? OrderCode { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    private PaymentSession()
    {
        // EF Core
    }

    public static PaymentSession Create(PaymentSessionDraft draft, DateTime nowUtc, TimeSpan lifetime)
    {
        if (draft.TotalAmount < 0)
        {
            throw new BusinessRuleValidationException("The total cannot be negative.");
        }

        return new PaymentSession
        {
            Id = Guid.NewGuid(),
            ReferenceCode = Guard.NotNullOrWhiteSpace(draft.ReferenceCode, nameof(draft.ReferenceCode)),
            Channel = draft.Channel,
            PaymentMethodCode = draft.PaymentMethodCode,
            PaymentMethodLabel = draft.PaymentMethodLabel,
            CustomerId = draft.CustomerId,
            CustomerName = draft.CustomerName,
            Phone = draft.Phone,
            Email = draft.Email,
            DeliveryMethodCode = draft.DeliveryMethodCode,
            DeliveryMethodLabel = draft.DeliveryMethodLabel,
            IsPickup = draft.IsPickup,
            DeliveryAddressSnapshot = draft.DeliveryAddress,
            WantsUtensils = draft.WantsUtensils,
            CustomerNote = draft.CustomerNote,
            Subtotal = draft.Subtotal,
            ShippingFee = draft.ShippingFee,
            Discount = draft.Discount,
            DiscountCode = draft.DiscountCode,
            PromotionId = draft.PromotionId,
            ShippingDiscount = draft.ShippingDiscount,
            TotalAmount = draft.TotalAmount,
            BankName = draft.BankName,
            BankAccountNumber = draft.BankAccountNumber,
            BankAccountHolder = draft.BankAccountHolder,
            IdempotencyKey = string.IsNullOrWhiteSpace(draft.IdempotencyKey) ? null : draft.IdempotencyKey.Trim(),
            RequestJson = draft.RequestJson,
            LinesJson = draft.LinesJson,
            ExpiresAtUtc = nowUtc + lifetime,
        };
    }

    public bool IsExpired(DateTime nowUtc) => Status == PaymentSessionStatus.Pending && nowUtc >= ExpiresAtUtc;

    /// <summary>A pending session past its time becomes cancelled, the first time anyone looks at it.
    /// Returns true when it just changed.</summary>
    public bool ExpireIfDue(DateTime nowUtc)
    {
        if (!IsExpired(nowUtc))
        {
            return false;
        }

        Status = PaymentSessionStatus.Cancelled;
        ResolutionNote = "Phiên thanh toán đã hết hạn.";
        return true;
    }

    /// <summary>The money arrived and the order was created from this session.</summary>
    public void MarkSucceeded(Guid orderId, string orderCode)
    {
        RequirePending();
        Status = PaymentSessionStatus.Success;
        OrderId = orderId;
        OrderCode = orderCode;
    }

    /// <summary>Staff could not find the money (or the customer reported a problem).</summary>
    public void MarkFailed(string? note)
    {
        RequirePending();
        Status = PaymentSessionStatus.Failed;
        ResolutionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>The customer gives up (or an admin abandons it) — no order was ever created.</summary>
    public void Cancel()
    {
        if (Status == PaymentSessionStatus.Success)
        {
            throw new BusinessRuleValidationException("A session that already created an order cannot be cancelled.");
        }

        if (Status != PaymentSessionStatus.Cancelled)
        {
            Status = PaymentSessionStatus.Cancelled;
            ResolutionNote ??= "Khách hàng đã hủy phiên thanh toán.";
        }
    }

    /// <summary>After a failure the customer may try again: the session is pending again with a new deadline.</summary>
    public void Retry(DateTime nowUtc, TimeSpan lifetime)
    {
        if (Status != PaymentSessionStatus.Failed)
        {
            throw new BusinessRuleValidationException($"A session in status '{EnumWire.ToWire(Status)}' cannot be retried.");
        }

        Status = PaymentSessionStatus.Pending;
        ResolutionNote = null;
        ExpiresAtUtc = nowUtc + lifetime;
    }

    private void RequirePending()
    {
        if (Status != PaymentSessionStatus.Pending)
        {
            throw new BusinessRuleValidationException($"The payment session is '{EnumWire.ToWire(Status)}', not waiting for payment.");
        }
    }
}

public sealed record PaymentSessionDraft(
    string ReferenceCode,
    PaymentChannel Channel,
    string PaymentMethodCode,
    string PaymentMethodLabel,
    Guid? CustomerId,
    string CustomerName,
    string Phone,
    string? Email,
    string DeliveryMethodCode,
    string DeliveryMethodLabel,
    bool IsPickup,
    string DeliveryAddress,
    bool WantsUtensils,
    string? CustomerNote,
    decimal Subtotal,
    decimal ShippingFee,
    decimal Discount,
    string? DiscountCode,
    Guid? PromotionId,
    decimal ShippingDiscount,
    decimal TotalAmount,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountHolder,
    string? IdempotencyKey,
    string RequestJson,
    string LinesJson);

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

public enum PaymentTransactionAction
{
    Created,
    Charge,
    Refund,
    Cancel,
    Retry,
}

public enum PaymentTransactionResult
{
    Success,
    Failed,
}

/// <summary>The money side of ONE order: how much, by which method, and whether it was received. Kept apart
/// from <see cref="Order"/> so a real payment gateway can later drive it (webhooks, refunds) without touching
/// the order. Every status change appends an immutable <see cref="PaymentTransaction"/>; "paid" is only ever
/// set by a person with the payments permission (or, later, a gateway) — never by the customer's browser.</summary>
public sealed class Payment : AuditableEntity
{
    private readonly List<PaymentTransaction> _transactions = [];

    public Guid OrderId { get; private set; }
    public string OrderCode { get; private set; } = string.Empty;
    public string PaymentMethodCode { get; private set; } = string.Empty;
    public string PaymentMethodLabel { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }
    public string? Gateway { get; private set; }
    public string? GatewayReference { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public IReadOnlyList<PaymentTransaction> Transactions => _transactions;

    private Payment()
    {
        // EF Core
    }

    /// <summary>Starts the payment of a new order as pending (COD) or, when the money was already
    /// confirmed (a confirmed payment session), as paid in the same step.</summary>
    public static Payment CreateFor(Order order, string? gateway, string? gatewayReference, DateTime nowUtc, string changedBy, bool alreadyPaid)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            PaymentMethodCode = order.PaymentMethodCode,
            PaymentMethodLabel = order.PaymentMethodLabel,
            Amount = order.TotalAmount,
            Gateway = gateway,
            GatewayReference = gatewayReference,
        };
        payment._transactions.Add(PaymentTransaction.Create(PaymentTransactionAction.Created, PaymentTransactionResult.Success, gateway, gatewayReference, "Khởi tạo giao dịch từ đơn hàng mới.", "Hệ thống", nowUtc));

        if (alreadyPaid)
        {
            payment.Transition(PaymentStatus.Paid, nowUtc, changedBy, "Đã xác nhận nhận được tiền.");
        }

        return payment;
    }

    /// <summary>Moves the payment along the state machine, appending the audit entry. The caller must also
    /// call <see cref="Order.SyncPaymentStatus"/> on the order.</summary>
    public void Transition(PaymentStatus to, DateTime nowUtc, string changedBy, string? note)
    {
        if (!PaymentStatusRules.CanTransition(Status, to))
        {
            throw new BusinessRuleValidationException($"A payment cannot move from '{EnumWire.ToWire(Status)}' to '{EnumWire.ToWire(to)}'.");
        }

        var (action, result) = to switch
        {
            PaymentStatus.Pending => (PaymentTransactionAction.Retry, PaymentTransactionResult.Success),
            PaymentStatus.Paid => (PaymentTransactionAction.Charge, PaymentTransactionResult.Success),
            PaymentStatus.Failed => (PaymentTransactionAction.Charge, PaymentTransactionResult.Failed),
            PaymentStatus.Refunded => (PaymentTransactionAction.Refund, PaymentTransactionResult.Success),
            _ => (PaymentTransactionAction.Cancel, PaymentTransactionResult.Success),
        };

        Status = to;
        if (to == PaymentStatus.Paid)
        {
            PaidAtUtc = nowUtc;
        }
        else if (to == PaymentStatus.Failed)
        {
            FailedAtUtc = nowUtc;
        }

        _transactions.Add(PaymentTransaction.Create(action, result, Gateway, GatewayReference, note, changedBy, nowUtc));
    }

    public IReadOnlyList<PaymentStatus> NextStatuses => PaymentStatusRules.NextStatuses(Status);
}

/// <summary>One audit entry of a payment. Append-only — never edited or deleted. Holds no card number, CVV
/// or secret, only a short outcome message.</summary>
public sealed class PaymentTransaction : Entity
{
    public Guid PaymentId { get; private set; }
    public PaymentTransactionAction Action { get; private set; }
    public PaymentTransactionResult Result { get; private set; }
    public string? Gateway { get; private set; }
    public string? GatewayReference { get; private set; }
    public string? Message { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private PaymentTransaction()
    {
        // EF Core
    }

    internal static PaymentTransaction Create(
        PaymentTransactionAction action,
        PaymentTransactionResult result,
        string? gateway,
        string? gatewayReference,
        string? message,
        string changedBy,
        DateTime atUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            Action = action,
            Result = result,
            Gateway = gateway,
            GatewayReference = gatewayReference,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            ChangedBy = changedBy,
            CreatedAtUtc = atUtc,
        };
}

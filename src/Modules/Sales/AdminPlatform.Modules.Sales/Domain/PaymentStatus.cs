namespace AdminPlatform.Modules.Sales.Domain;

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded,
    Cancelled,
}

/// <summary>The one payment state machine, independent of the order's: a completed order can still have
/// its payment refunded later. Refunded and Cancelled are final.</summary>
public static class PaymentStatusRules
{
    public static IReadOnlyList<PaymentStatus> NextStatuses(PaymentStatus from) => from switch
    {
        PaymentStatus.Pending => [PaymentStatus.Paid, PaymentStatus.Failed, PaymentStatus.Cancelled],
        PaymentStatus.Paid => [PaymentStatus.Refunded],
        PaymentStatus.Failed => [PaymentStatus.Pending, PaymentStatus.Cancelled],
        _ => [],
    };

    public static bool CanTransition(PaymentStatus from, PaymentStatus to) => NextStatuses(from).Contains(to);
}

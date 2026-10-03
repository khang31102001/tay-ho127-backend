namespace AdminPlatform.Modules.Sales.Domain;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    Ready,
    Delivering,
    Completed,
    Cancelled,
}

/// <summary>The one order state machine; every caller (service, response DTO) asks it. Completed and
/// Cancelled are final. A pickup order skips "delivering": ready → completed.</summary>
public static class OrderStatusRules
{
    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus from, bool isPickup) => from switch
    {
        OrderStatus.Pending => [OrderStatus.Confirmed, OrderStatus.Cancelled],
        OrderStatus.Confirmed => [OrderStatus.Preparing, OrderStatus.Cancelled],
        OrderStatus.Preparing => [OrderStatus.Ready, OrderStatus.Cancelled],
        OrderStatus.Ready => isPickup ? [OrderStatus.Completed, OrderStatus.Cancelled] : [OrderStatus.Delivering, OrderStatus.Cancelled],
        OrderStatus.Delivering => [OrderStatus.Completed, OrderStatus.Cancelled],
        _ => [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to, bool isPickup) => NextStatuses(from, isPickup).Contains(to);
}

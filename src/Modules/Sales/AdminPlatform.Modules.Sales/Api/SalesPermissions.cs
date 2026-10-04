using AdminPlatform.Modules.Sales.Application;

namespace AdminPlatform.Modules.Sales.Api;

public static class SalesPermissions
{
    public const string DeliveryMethodsView = "delivery-methods.view";
    public const string DeliveryMethodsCreate = "delivery-methods.create";
    public const string DeliveryMethodsUpdate = "delivery-methods.update";
    public const string DeliveryMethodsDelete = "delivery-methods.delete";

    public const string PaymentMethodsView = "payment-methods.view";
    public const string PaymentMethodsCreate = "payment-methods.create";
    public const string PaymentMethodsUpdate = "payment-methods.update";
    public const string PaymentMethodsDelete = "payment-methods.delete";

    public const string OrderOptionGroupsView = "order-option-groups.view";
    public const string OrderOptionGroupsCreate = "order-option-groups.create";
    public const string OrderOptionGroupsUpdate = "order-option-groups.update";
    public const string OrderOptionGroupsDelete = "order-option-groups.delete";

    public const string OrderSettingsView = "order-settings.view";
    public const string OrderSettingsUpdate = "order-settings.update";

    public const string OrdersView = "orders.view";

    /// <summary>Moves an order along its states (confirm, prepare, ship, complete). Cancelling needs <see cref="OrdersCancel"/> too.</summary>
    public const string OrdersUpdateStatus = "orders.update-status";

    public const string OrdersCancel = SalesPermissionCodes.OrdersCancel;

    public const string PaymentsView = "payments.view";

    /// <summary>Records money received / failed / refunded and confirms or rejects a payment session — handling money.</summary>
    public const string PaymentsManage = "payments.manage";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (DeliveryMethodsView, "View delivery methods"),
        (DeliveryMethodsCreate, "Create delivery methods"),
        (DeliveryMethodsUpdate, "Update delivery methods"),
        (DeliveryMethodsDelete, "Delete delivery methods"),
        (PaymentMethodsView, "View payment methods"),
        (PaymentMethodsCreate, "Create payment methods"),
        (PaymentMethodsUpdate, "Update payment methods"),
        (PaymentMethodsDelete, "Delete payment methods"),
        (OrderOptionGroupsView, "View general order options"),
        (OrderOptionGroupsCreate, "Create general order options"),
        (OrderOptionGroupsUpdate, "Update general order options"),
        (OrderOptionGroupsDelete, "Delete general order options"),
        (OrderSettingsView, "View order settings (order code format, payment session time)"),
        (OrderSettingsUpdate, "Update order settings"),
        (OrdersView, "View orders"),
        (OrdersUpdateStatus, "Change the status of orders (confirm, prepare, deliver, complete)"),
        (OrdersCancel, "Cancel orders"),
        (PaymentsView, "View payments and payment sessions"),
        (PaymentsManage, "Record, refund and confirm payments"),
    ];
}

namespace AdminPlatform.Modules.Sales.Application;

/// <summary>Permission codes the Application layer itself must check (everything else is enforced on the
/// controllers). The Api layer's SalesPermissions re-exports these.</summary>
public static class SalesPermissionCodes
{
    public const string OrdersCancel = "orders.cancel";
}

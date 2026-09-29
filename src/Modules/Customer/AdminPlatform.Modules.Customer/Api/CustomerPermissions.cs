namespace AdminPlatform.Modules.Customer.Api;

/// <summary>Permissions for the ADMIN side of the Customer module (/api/v1/customers/...). The customer's
/// own self-service endpoints (/api/v1/customers/me/...) are gated by account type, not by these.</summary>
public static class CustomerPermissions
{
    public const string CustomersView = "customers.view";
    public const string CustomersCreate = "customers.create";
    public const string CustomersUpdate = "customers.update";
    public const string CustomersManageAddresses = "customers.addresses.manage";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (CustomersView, "View customers"),
        (CustomersCreate, "Create customers"),
        (CustomersUpdate, "Update customers"),
        (CustomersManageAddresses, "Manage customer addresses"),
    ];
}

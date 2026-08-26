namespace AdminPlatform.Modules.Organization.Api;

public static class OrganizationPermissions
{
    public const string OrganizationsView = "organizations.view";
    public const string OrganizationsCreate = "organizations.create";
    public const string OrganizationsUpdate = "organizations.update";

    public const string DepartmentsView = "departments.view";
    public const string DepartmentsCreate = "departments.create";
    public const string DepartmentsUpdate = "departments.update";

    public const string BrandsView = "brands.view";
    public const string BrandsCreate = "brands.create";
    public const string BrandsUpdate = "brands.update";

    public const string UsersManageDepartments = "users.departments.manage";
    public const string UsersManageBrands = "users.brands.manage";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (OrganizationsView, "View organizations"),
        (OrganizationsCreate, "Create organizations"),
        (OrganizationsUpdate, "Update organizations"),
        (DepartmentsView, "View departments"),
        (DepartmentsCreate, "Create departments"),
        (DepartmentsUpdate, "Update departments"),
        (BrandsView, "View brands"),
        (BrandsCreate, "Create brands"),
        (BrandsUpdate, "Update brands"),
        (UsersManageDepartments, "Assign departments to a user"),
        (UsersManageBrands, "Assign brands to a user"),
    ];
}

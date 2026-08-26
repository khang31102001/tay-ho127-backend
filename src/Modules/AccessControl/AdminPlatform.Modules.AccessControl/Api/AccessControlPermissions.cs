namespace AdminPlatform.Modules.AccessControl.Api;

public static class AccessControlPermissions
{
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesManagePermissions = "roles.permissions.manage";

    public const string PermissionsView = "permissions.view";
    public const string PermissionsCreate = "permissions.create";
    public const string PermissionsUpdate = "permissions.update";
    public const string PermissionsDelete = "permissions.delete";

    public const string UsersManageRoles = "users.roles.manage";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (RolesView, "View roles"),
        (RolesCreate, "Create roles"),
        (RolesUpdate, "Update roles"),
        (RolesDelete, "Delete roles"),
        (RolesManagePermissions, "Assign permissions to a role"),
        (PermissionsView, "View permissions"),
        (PermissionsCreate, "Create permissions"),
        (PermissionsUpdate, "Update permissions"),
        (PermissionsDelete, "Delete permissions"),
        (UsersManageRoles, "Assign roles to a user"),
    ];
}

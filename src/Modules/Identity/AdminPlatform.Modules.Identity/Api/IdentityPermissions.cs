namespace AdminPlatform.Modules.Identity.Api;

/// <summary>Permission codes this module's endpoints are guarded by. Collected by the Migrator (the one
/// tool allowed to reference every module) and seeded into AccessControl's Permissions table — see
/// AdminPlatform.Migrator/Seeding/PermissionCatalog.cs.</summary>
public static class IdentityPermissions
{
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersResetPassword = "users.reset-password";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (UsersView, "View users"),
        (UsersCreate, "Create users"),
        (UsersUpdate, "Update users"),
        (UsersResetPassword, "Reset a user's password"),
    ];
}

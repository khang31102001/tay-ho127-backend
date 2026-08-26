namespace AdminPlatform.Migrator;

/// <summary>Aggregates every module's permission-code list. Only the Migrator is allowed to reference all
/// modules, so this is the one place the full cross-module permission catalog can be assembled to seed
/// into AccessControl's Permissions table — see architecture assumption #6.</summary>
internal static class PermissionCatalog
{
    public static IReadOnlyCollection<(string Code, string Description)> All { get; } =
    [
        .. AdminPlatform.Modules.Identity.Api.IdentityPermissions.All,
        .. AdminPlatform.Modules.AccessControl.Api.AccessControlPermissions.All,
        .. AdminPlatform.Modules.Organization.Api.OrganizationPermissions.All,
        .. AdminPlatform.Modules.Navigation.Api.NavigationPermissions.All,
        .. AdminPlatform.Modules.Platform.Api.PlatformPermissions.All,
    ];
}

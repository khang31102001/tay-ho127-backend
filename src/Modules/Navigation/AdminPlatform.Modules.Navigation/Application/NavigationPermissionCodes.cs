namespace AdminPlatform.Modules.Navigation.Application;

/// <summary>Permission codes the Application layer checks itself (Application must not depend on Api). The Api
/// layer's <c>NavigationPermissions</c> re-exports them for controller attributes and the permission catalog.</summary>
public static class NavigationPermissionCodes
{
    // Manage the ADMIN sidebar. Kept under the original `menus.*` codes so existing roles keep working.
    public const string MenusView = "menus.view";
    public const string MenusCreate = "menus.create";
    public const string MenusUpdate = "menus.update";
    public const string MenusDelete = "menus.delete";

    /// <summary>Assign permissions to a navigation item — used for both scopes.</summary>
    public const string MenusManagePermissions = "menus.permissions.manage";

    // Manage the public WEBSITE menus. Separate from `menus.*` so a content editor can maintain them without
    // being able to touch the admin sidebar.
    public const string SiteNavigationView = "site-navigation.view";
    public const string SiteNavigationCreate = "site-navigation.create";
    public const string SiteNavigationUpdate = "site-navigation.update";
    public const string SiteNavigationDelete = "site-navigation.delete";
}

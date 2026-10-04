using AdminPlatform.Modules.Navigation.Application;

namespace AdminPlatform.Modules.Navigation.Api;

public static class NavigationPermissions
{
    public const string MenusView = NavigationPermissionCodes.MenusView;
    public const string MenusCreate = NavigationPermissionCodes.MenusCreate;
    public const string MenusUpdate = NavigationPermissionCodes.MenusUpdate;
    public const string MenusDelete = NavigationPermissionCodes.MenusDelete;
    public const string MenusManagePermissions = NavigationPermissionCodes.MenusManagePermissions;

    public const string SiteNavigationView = NavigationPermissionCodes.SiteNavigationView;
    public const string SiteNavigationCreate = NavigationPermissionCodes.SiteNavigationCreate;
    public const string SiteNavigationUpdate = NavigationPermissionCodes.SiteNavigationUpdate;
    public const string SiteNavigationDelete = NavigationPermissionCodes.SiteNavigationDelete;

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (MenusView, "View admin menus"),
        (MenusCreate, "Create admin menu items"),
        (MenusUpdate, "Update admin menu items"),
        (MenusDelete, "Delete admin menu items"),
        (MenusManagePermissions, "Assign permissions to a navigation item"),
        (SiteNavigationView, "View website navigation menus"),
        (SiteNavigationCreate, "Create website navigation items"),
        (SiteNavigationUpdate, "Update website navigation menus and items"),
        (SiteNavigationDelete, "Delete website navigation items"),
    ];
}

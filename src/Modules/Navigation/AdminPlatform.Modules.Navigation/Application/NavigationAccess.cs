using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Application;

public enum NavigationAction
{
    View,
    Create,
    Update,
    Delete,
}

/// <summary>Which permission lets a caller manage a navigation menu depends on its scope: `menus.*` for the admin
/// sidebar, `site-navigation.*` for the website menus. A controller attribute can only name one code, so the
/// check lives here and runs once the menu (hence its scope) is known.</summary>
public static class NavigationAccess
{
    public static string RequiredPermission(NavigationScope scope, NavigationAction action) => (scope, action) switch
    {
        (NavigationScope.Admin, NavigationAction.View) => NavigationPermissionCodes.MenusView,
        (NavigationScope.Admin, NavigationAction.Create) => NavigationPermissionCodes.MenusCreate,
        (NavigationScope.Admin, NavigationAction.Update) => NavigationPermissionCodes.MenusUpdate,
        (NavigationScope.Admin, NavigationAction.Delete) => NavigationPermissionCodes.MenusDelete,
        (NavigationScope.Site, NavigationAction.View) => NavigationPermissionCodes.SiteNavigationView,
        (NavigationScope.Site, NavigationAction.Create) => NavigationPermissionCodes.SiteNavigationCreate,
        (NavigationScope.Site, NavigationAction.Update) => NavigationPermissionCodes.SiteNavigationUpdate,
        (NavigationScope.Site, NavigationAction.Delete) => NavigationPermissionCodes.SiteNavigationDelete,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static bool Can(ICurrentUser user, NavigationScope scope, NavigationAction action) =>
        user.HasPermission(RequiredPermission(scope, action));

    public static void Require(ICurrentUser user, NavigationScope scope, NavigationAction action)
    {
        if (!Can(user, scope, action))
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện thao tác này trên menu.");
        }
    }
}

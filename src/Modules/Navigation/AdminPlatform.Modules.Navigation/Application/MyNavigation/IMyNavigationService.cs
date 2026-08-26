using AdminPlatform.Modules.Navigation.Application.Menus;

namespace AdminPlatform.Modules.Navigation.Application.MyNavigation;

/// <summary>Builds the menu tree visible to one caller. Permission codes are the ones already embedded
/// in their JWT (ICurrentUser.Permissions) — no cross-module lookup needed at read time.</summary>
public interface IMyNavigationService
{
    Task<IReadOnlyList<MenuTreeNode>> GetVisibleMenuTreeAsync(
        IReadOnlyCollection<string> callerPermissions, CancellationToken cancellationToken);
}

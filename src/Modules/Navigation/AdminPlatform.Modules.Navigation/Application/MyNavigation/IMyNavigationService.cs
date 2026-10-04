namespace AdminPlatform.Modules.Navigation.Application.MyNavigation;

/// <summary>One node of the admin sidebar shown to a caller. `Route` is null for a group heading.</summary>
public sealed record MenuTreeNode(
    Guid Id, string Code, string Name, string? Route, string? Icon, int SortOrder, IReadOnlyList<MenuTreeNode> Children);

/// <summary>Builds the admin sidebar tree visible to one caller. Permission codes are the ones already embedded
/// in their JWT (ICurrentUser.Permissions) — no cross-module lookup needed at read time.</summary>
public interface IMyNavigationService
{
    Task<IReadOnlyList<MenuTreeNode>> GetVisibleMenuTreeAsync(
        IReadOnlyCollection<string> callerPermissions, CancellationToken cancellationToken);
}

using AdminPlatform.Modules.Navigation.Application.Menus;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application.MyNavigation;

public sealed class MyNavigationService : IMyNavigationService
{
    private readonly INavigationDbContext _db;

    public MyNavigationService(INavigationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MenuTreeNode>> GetVisibleMenuTreeAsync(
        IReadOnlyCollection<string> callerPermissions, CancellationToken cancellationToken)
    {
        var menus = await _db.Menus.AsNoTracking().Where(m => m.IsActive).ToListAsync(cancellationToken);
        var permissionLinks = await _db.MenuPermissions.AsNoTracking().ToListAsync(cancellationToken);

        var requiredByMenu = permissionLinks
            .GroupBy(mp => mp.MenuId)
            .ToDictionary(g => g.Key, g => g.Select(mp => mp.PermissionCode).ToList());

        var callerSet = callerPermissions.ToHashSet();

        return FilterTree(menus, requiredByMenu, callerSet, null);
    }

    private static List<MenuTreeNode> FilterTree(
        List<Menu> all,
        IReadOnlyDictionary<Guid, List<string>> requiredByMenu,
        HashSet<string> callerPermissions,
        Guid? parentId)
    {
        var result = new List<MenuTreeNode>();

        foreach (var menu in all.Where(m => m.ParentId == parentId).OrderBy(m => m.SortOrder).ThenBy(m => m.Name))
        {
            var children = FilterTree(all, requiredByMenu, callerPermissions, menu.Id);
            var required = requiredByMenu.TryGetValue(menu.Id, out var codes) ? codes : [];
            var selfVisible = required.Count == 0 || required.Any(callerPermissions.Contains);

            if (!selfVisible)
            {
                continue;
            }

            if (string.IsNullOrEmpty(menu.Route) && children.Count == 0)
            {
                continue;
            }

            result.Add(new MenuTreeNode(menu.Id, menu.Code, menu.Name, menu.Route, menu.Icon, menu.SortOrder, children));
        }

        return result;
    }
}

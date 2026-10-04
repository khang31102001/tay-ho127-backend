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
        // Only the ADMIN scope: a website menu never shows up in the admin sidebar.
        var adminMenuIds = _db.NavigationMenus
            .Where(m => m.Scope == NavigationScope.Admin && m.IsActive)
            .Select(m => m.Id);

        var items = await _db.NavigationItems.AsNoTracking()
            .Where(i => i.IsActive && adminMenuIds.Contains(i.MenuId))
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();
        var permissionLinks = await _db.NavigationItemPermissions.AsNoTracking()
            .Where(p => itemIds.Contains(p.ItemId))
            .ToListAsync(cancellationToken);

        var requiredByItem = permissionLinks
            .GroupBy(p => p.ItemId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PermissionCode).ToList());

        return FilterTree(items, requiredByItem, callerPermissions.ToHashSet(), null);
    }

    private static List<MenuTreeNode> FilterTree(
        List<NavigationItem> all,
        IReadOnlyDictionary<Guid, List<string>> requiredByItem,
        HashSet<string> callerPermissions,
        Guid? parentId)
    {
        var result = new List<MenuTreeNode>();

        foreach (var item in all.Where(i => i.ParentId == parentId).OrderBy(i => i.SortOrder).ThenBy(i => i.Name))
        {
            var children = FilterTree(all, requiredByItem, callerPermissions, item.Id);
            var required = requiredByItem.TryGetValue(item.Id, out var codes) ? codes : [];
            var selfVisible = required.Count == 0 || required.Any(callerPermissions.Contains);

            if (!selfVisible)
            {
                continue;
            }

            // A heading (or any entry without a link) is only worth showing while something under it is visible.
            if (string.IsNullOrEmpty(item.Url) && children.Count == 0)
            {
                continue;
            }

            result.Add(new MenuTreeNode(item.Id, item.Code, item.Name, item.Url, item.Icon, item.SortOrder, children));
        }

        return result;
    }
}

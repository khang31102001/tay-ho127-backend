using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application.Public;

public sealed class PublicNavigationService : IPublicNavigationService
{
    private readonly INavigationDbContext _db;

    public PublicNavigationService(INavigationDbContext db)
    {
        _db = db;
    }

    public async Task<PublicNavigationResponse> GetByLocationAsync(string location, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NavigationLocation>(location, ignoreCase: true, out var parsed)
            || !NavigationMenu.IsValidPlacement(NavigationScope.Site, parsed))
        {
            throw new NotFoundException("Navigation location", location);
        }

        var menu = await _db.NavigationMenus.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Scope == NavigationScope.Site && m.Location == parsed, cancellationToken);
        if (menu is null || !menu.IsActive)
        {
            return new PublicNavigationResponse(menu?.Code ?? string.Empty, menu?.Name ?? string.Empty, parsed.ToString().ToLowerInvariant(), []);
        }

        var items = await _db.NavigationItems.AsNoTracking()
            .Where(i => i.MenuId == menu.Id && i.IsActive)
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();

        // Gated items are not public. Today nothing can be gated on the website, but this keeps an item hidden from
        // anonymous visitors the moment someone assigns it a permission.
        var gatedIds = (await _db.NavigationItemPermissions.AsNoTracking()
                .Where(p => itemIds.Contains(p.ItemId))
                .Select(p => p.ItemId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var details = await _db.NavigationItemSiteDetails.AsNoTracking()
            .Where(d => itemIds.Contains(d.ItemId))
            .ToDictionaryAsync(d => d.ItemId, cancellationToken);

        var publicItems = items.Where(i => !gatedIds.Contains(i.Id)).ToList();
        var tree = BuildTree(publicItems, details, null, 1);

        return new PublicNavigationResponse(menu.Code, menu.Name, parsed.ToString().ToLowerInvariant(), tree);
    }

    private static List<PublicNavigationNode> BuildTree(
        List<NavigationItem> all, IReadOnlyDictionary<Guid, NavigationItemSiteDetail> details, Guid? parentId, int depth)
    {
        var result = new List<PublicNavigationNode>();
        if (depth > NavigationTreeRules.MaxDepth)
        {
            return result;
        }

        foreach (var item in all.Where(i => i.ParentId == parentId).OrderBy(i => i.SortOrder).ThenBy(i => i.Name))
        {
            var children = BuildTree(all, details, item.Id, depth + 1);

            // A heading with nothing under it, or a link without a target, has nothing to show a visitor.
            details.TryGetValue(item.Id, out var detail);
            if (item.IsGroup ? children.Count == 0 : detail is null)
            {
                continue;
            }

            result.Add(new PublicNavigationNode(
                item.Id, item.Name, item.Url, detail?.TargetType.ToString().ToLowerInvariant(), detail?.TargetId,
                detail?.OpenInNewTab ?? false, item.Icon, item.SortOrder, children));
        }

        return result;
    }
}

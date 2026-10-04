using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Navigation.Application.Items;

public sealed class NavigationItemService : INavigationItemService
{
    private readonly INavigationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly NavigationOptions _options;

    public NavigationItemService(INavigationDbContext db, ICurrentUser currentUser, IOptions<NavigationOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<PagedResult<NavigationItemResponse>> ListAsync(
        Guid? menuId, PagedRequest request, CancellationToken cancellationToken)
    {
        if (menuId is { } id)
        {
            var menu = await FindMenuOrThrowAsync(id, cancellationToken);
            NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.View);
        }

        var viewableScopes = Enum.GetValues<NavigationScope>()
            .Where(scope => NavigationAccess.Can(_currentUser, scope, NavigationAction.View))
            .ToList();
        if (viewableScopes.Count == 0)
        {
            throw new ForbiddenException("Bạn không có quyền xem menu.");
        }

        var query =
            from item in _db.NavigationItems.AsNoTracking()
            join menu in _db.NavigationMenus.AsNoTracking() on item.MenuId equals menu.Id
            where viewableScopes.Contains(menu.Scope) && (menuId == null || item.MenuId == menuId)
            select item;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(i => EF.Functions.ILike(i.Code, pattern) || EF.Functions.ILike(i.Name, pattern));
        }

        var page = await query.OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToPagedResultAsync(request, cancellationToken);

        var ids = page.Items.Select(i => i.Id).ToList();
        var details = await _db.NavigationItemSiteDetails.AsNoTracking()
            .Where(d => ids.Contains(d.ItemId))
            .ToDictionaryAsync(d => d.ItemId, cancellationToken);

        return new PagedResult<NavigationItemResponse>(
            page.Items.Select(i => ToResponse(i, details.GetValueOrDefault(i.Id))).ToList(),
            page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<NavigationItemResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindItemOrThrowAsync(id, cancellationToken);
        var menu = await FindMenuOrThrowAsync(item.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.View);

        var detail = await _db.NavigationItemSiteDetails.AsNoTracking().SingleOrDefaultAsync(d => d.ItemId == id, cancellationToken);
        return ToResponse(item, detail);
    }

    public async Task<NavigationItemResponse> CreateAsync(CreateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        var menu = await FindMenuOrThrowAsync(request.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Create);

        var code = string.IsNullOrWhiteSpace(request.Code) ? $"item-{Guid.NewGuid():n}"[..13] : request.Code.Trim();
        if (await _db.NavigationItems.AnyAsync(i => i.MenuId == menu.Id && i.Code == code, cancellationToken))
        {
            throw new ConflictException($"A menu item with code '{code}' already exists in this menu.");
        }

        var site = ValidateShape(menu.Scope, request.IsGroup, request.Url, request.Site);

        var parentById = await LoadParentMapAsync(menu.Id, cancellationToken);
        EnsureParentAllowsChild(request.ParentId, parentById, subtreeHeight: 1);

        var item = NavigationItem.Create(menu.Id, code, request.Label, request.ParentId, request.IsGroup, request.Url, request.Icon, request.SortOrder);
        _db.NavigationItems.Add(item);

        NavigationItemSiteDetail? detail = null;
        if (site is not null)
        {
            detail = NavigationItemSiteDetail.Create(item.Id, site.Value.TargetType, site.Value.TargetId, site.Value.OpenInNewTab);
            _db.NavigationItemSiteDetails.Add(detail);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(item, detail);
    }

    public async Task<NavigationItemResponse> UpdateAsync(Guid id, UpdateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        var item = await FindItemOrThrowAsync(id, cancellationToken);
        var menu = await FindMenuOrThrowAsync(item.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Update);

        var site = ValidateShape(menu.Scope, request.IsGroup, request.Url, request.Site);

        var parentById = await LoadParentMapAsync(menu.Id, cancellationToken);
        if (request.ParentId is { } parentId && NavigationTreeRules.IsSelfOrDescendant(parentId, id, parentById))
        {
            throw new BusinessRuleValidationException("A menu item cannot be moved under itself or one of its descendants.");
        }

        EnsureParentAllowsChild(request.ParentId, parentById, NavigationTreeRules.SubtreeHeight(id, parentById));

        item.Update(request.Label, request.IsActive, request.ParentId, request.IsGroup, request.Url, request.Icon, request.SortOrder);

        var detail = await _db.NavigationItemSiteDetails.SingleOrDefaultAsync(d => d.ItemId == id, cancellationToken);
        if (site is null)
        {
            if (detail is not null)
            {
                _db.NavigationItemSiteDetails.Remove(detail);
                detail = null;
            }
        }
        else if (detail is null)
        {
            detail = NavigationItemSiteDetail.Create(id, site.Value.TargetType, site.Value.TargetId, site.Value.OpenInNewTab);
            _db.NavigationItemSiteDetails.Add(detail);
        }
        else
        {
            detail.Update(site.Value.TargetType, site.Value.TargetId, site.Value.OpenInNewTab);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(item, detail);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindItemOrThrowAsync(id, cancellationToken);
        var menu = await FindMenuOrThrowAsync(item.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Delete);

        if (await _db.NavigationItems.AnyAsync(i => i.ParentId == id, cancellationToken))
        {
            throw new ConflictException("This menu item still has child items and cannot be deleted.");
        }

        _db.NavigationItemPermissions.RemoveRange(await _db.NavigationItemPermissions.Where(p => p.ItemId == id).ToListAsync(cancellationToken));
        _db.NavigationItemSiteDetails.RemoveRange(await _db.NavigationItemSiteDetails.Where(d => d.ItemId == id).ToListAsync(cancellationToken));
        _db.NavigationItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderAsync(ReorderNavigationItemsRequest request, CancellationToken cancellationToken)
    {
        var menu = await FindMenuOrThrowAsync(request.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Update);

        var orderedIds = request.OrderedItemIds.Distinct().ToList();
        var items = await _db.NavigationItems.Where(i => i.MenuId == menu.Id).ToListAsync(cancellationToken);
        var byId = items.ToDictionary(i => i.Id);

        if (orderedIds.Any(id => !byId.ContainsKey(id)))
        {
            throw new BusinessRuleValidationException("One or more menu items do not belong to this menu.");
        }

        var parentById = items.ToDictionary(i => i.Id, i => i.ParentId);
        if (request.ParentId is { } newParentId)
        {
            if (!byId.ContainsKey(newParentId))
            {
                throw new BusinessRuleValidationException("The parent menu item does not belong to this menu.");
            }

            if (orderedIds.Any(id => NavigationTreeRules.IsSelfOrDescendant(newParentId, id, parentById)))
            {
                throw new BusinessRuleValidationException("A menu item cannot be moved under itself or one of its descendants.");
            }

            var parentDepth = NavigationTreeRules.Depth(newParentId, parentById);
            if (orderedIds.Any(id => parentDepth + NavigationTreeRules.SubtreeHeight(id, parentById) > NavigationTreeRules.MaxDepth))
            {
                throw new BusinessRuleValidationException($"A menu can be at most {NavigationTreeRules.MaxDepth} levels deep.");
            }
        }

        for (var index = 0; index < orderedIds.Count; index++)
        {
            byId[orderedIds[index]].Place(request.ParentId, index + 1);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindItemOrThrowAsync(id, cancellationToken);
        var menu = await FindMenuOrThrowAsync(item.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.View);

        return await _db.NavigationItemPermissions.Where(p => p.ItemId == id).Select(p => p.PermissionCode).ToListAsync(cancellationToken);
    }

    public async Task SetPermissionsAsync(Guid id, AssignNavigationItemPermissionsRequest request, CancellationToken cancellationToken)
    {
        var item = await FindItemOrThrowAsync(id, cancellationToken);
        var menu = await FindMenuOrThrowAsync(item.MenuId, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Update);
        if (!_currentUser.HasPermission(NavigationPermissionCodes.MenusManagePermissions))
        {
            throw new ForbiddenException("Bạn không có quyền gán quyền truy cập cho menu.");
        }

        var requested = request.PermissionCodes.Select(code => code.Trim()).Where(code => code.Length > 0).ToHashSet();
        if (requested.Any(code => code.StartsWith("group:", StringComparison.Ordinal)))
        {
            throw new BusinessRuleValidationException("Only permission codes can gate a menu item, not permission groups.");
        }

        if (menu.Scope == NavigationScope.Site && requested.Count > 0 && !_options.AllowSitePermissions)
        {
            throw new BusinessRuleValidationException("Website menu items are public; assigning permissions to them is not enabled yet.");
        }

        var current = await _db.NavigationItemPermissions.Where(p => p.ItemId == id).ToListAsync(cancellationToken);
        var currentCodes = current.Select(p => p.PermissionCode).ToHashSet();

        _db.NavigationItemPermissions.RemoveRange(current.Where(p => !requested.Contains(p.PermissionCode)));
        foreach (var code in requested.Except(currentCodes))
        {
            _db.NavigationItemPermissions.Add(NavigationItemPermission.Create(id, code));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Checks the shape rules for the menu's scope and returns the parsed website detail (null when none applies).</summary>
    private static (NavigationTargetType TargetType, Guid? TargetId, bool OpenInNewTab)? ValidateShape(
        NavigationScope scope, bool isGroup, string? url, NavigationSiteDetailRequest? site)
    {
        var hasUrl = !string.IsNullOrWhiteSpace(url);

        if (isGroup)
        {
            if (hasUrl || site is not null)
            {
                throw new BusinessRuleValidationException("A group heading has no link: leave the URL and the website target empty.");
            }

            return null;
        }

        if (scope == NavigationScope.Admin)
        {
            if (site is not null)
            {
                throw new BusinessRuleValidationException("Admin menu items have no website target.");
            }

            if (!hasUrl || !url!.Trim().StartsWith('/'))
            {
                throw new BusinessRuleValidationException("An admin menu item needs a route starting with '/'.");
            }

            return null;
        }

        if (site is null)
        {
            throw new BusinessRuleValidationException("A website menu item needs a target (route, page or external URL).");
        }

        var targetType = Enum.Parse<NavigationTargetType>(site.TargetType, ignoreCase: true);
        switch (targetType)
        {
            case NavigationTargetType.Route when !hasUrl || !url!.Trim().StartsWith('/'):
                throw new BusinessRuleValidationException("An internal route must start with '/'.");
            case NavigationTargetType.External when !hasUrl || !IsHttpUrl(url!):
                throw new BusinessRuleValidationException("An external link must be a full http(s) URL.");
            case NavigationTargetType.Page when site.TargetId is null || site.TargetId == Guid.Empty:
                throw new BusinessRuleValidationException("Choose the CMS page this item links to.");
        }

        return (targetType, site.TargetId, site.OpenInNewTab);
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static void EnsureParentAllowsChild(Guid? parentId, IReadOnlyDictionary<Guid, Guid?> parentById, int subtreeHeight)
    {
        if (parentId is not { } id)
        {
            return;
        }

        if (!parentById.ContainsKey(id))
        {
            throw new BusinessRuleValidationException("The parent menu item does not exist in this menu.");
        }

        if (NavigationTreeRules.Depth(id, parentById) + subtreeHeight > NavigationTreeRules.MaxDepth)
        {
            throw new BusinessRuleValidationException($"A menu can be at most {NavigationTreeRules.MaxDepth} levels deep.");
        }
    }

    private async Task<Dictionary<Guid, Guid?>> LoadParentMapAsync(Guid menuId, CancellationToken cancellationToken) =>
        await _db.NavigationItems.AsNoTracking()
            .Where(i => i.MenuId == menuId)
            .Select(i => new { i.Id, i.ParentId })
            .ToDictionaryAsync(i => i.Id, i => i.ParentId, cancellationToken);

    private async Task<NavigationMenu> FindMenuOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.NavigationMenus.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(NavigationMenu), id);

    private async Task<NavigationItem> FindItemOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.NavigationItems.SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(NavigationItem), id);

    private static NavigationItemResponse ToResponse(NavigationItem item, NavigationItemSiteDetail? detail) =>
        new(item.Id, item.MenuId, item.ParentId, item.Code, item.Name, item.IsActive, item.IsGroup, item.Url, item.Icon, item.SortOrder,
            detail is null ? null : new NavigationSiteDetailResponse(detail.TargetType.ToString().ToLowerInvariant(), detail.TargetId, detail.OpenInNewTab));
}

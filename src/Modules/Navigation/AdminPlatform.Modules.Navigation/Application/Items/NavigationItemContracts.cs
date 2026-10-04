namespace AdminPlatform.Modules.Navigation.Application.Items;

/// <summary>Website-only link details. TargetType is "route" | "page" | "external".</summary>
public sealed record NavigationSiteDetailRequest(string TargetType, Guid? TargetId, bool OpenInNewTab);

public sealed record NavigationSiteDetailResponse(string TargetType, Guid? TargetId, bool OpenInNewTab);

/// <summary>Code is optional (generated when empty) and immutable afterwards. Url is the admin route, or the route /
/// external URL of a website link; a group heading has no Url. Site is required for a website link item and must
/// be absent for admin items and group headings.</summary>
public sealed record CreateNavigationItemRequest(
    Guid MenuId, string? Code, string Label, Guid? ParentId, bool IsGroup, string? Url, string? Icon, int SortOrder,
    NavigationSiteDetailRequest? Site);

public sealed record UpdateNavigationItemRequest(
    string Label, bool IsActive, Guid? ParentId, bool IsGroup, string? Url, string? Icon, int SortOrder,
    NavigationSiteDetailRequest? Site);

public sealed record NavigationItemResponse(
    Guid Id, Guid MenuId, Guid? ParentId, string Code, string Label, bool IsActive, bool IsGroup, string? Url,
    string? Icon, int SortOrder, NavigationSiteDetailResponse? Site);

/// <summary>Puts the items — in exactly this order — under ParentId (null = top level) and renumbers their sort order 1..N.</summary>
public sealed record ReorderNavigationItemsRequest(Guid MenuId, Guid? ParentId, IReadOnlyList<Guid> OrderedItemIds);

public sealed record AssignNavigationItemPermissionsRequest(IReadOnlyCollection<string> PermissionCodes);

namespace AdminPlatform.Modules.Navigation.Application.Menus;

public sealed record CreateMenuRequest(string Code, string Name, Guid? ParentId, string? Route, string? Icon, int SortOrder);

public sealed record UpdateMenuRequest(string Name, bool IsActive, Guid? ParentId, string? Route, string? Icon, int SortOrder);

public sealed record MenuResponse(
    Guid Id, string Code, string Name, bool IsActive, Guid? ParentId, string? Route, string? Icon, int SortOrder);

public sealed record MenuTreeNode(
    Guid Id, string Code, string Name, string? Route, string? Icon, int SortOrder, IReadOnlyList<MenuTreeNode> Children);

public sealed record AssignMenuPermissionsRequest(IReadOnlyCollection<string> PermissionCodes);

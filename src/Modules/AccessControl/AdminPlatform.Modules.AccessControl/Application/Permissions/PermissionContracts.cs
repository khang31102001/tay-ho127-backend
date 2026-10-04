namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

/// <summary>A leaf needs a parent group; a group's parent is optional (null = a top-level module group).
/// A leaf Code looks like "users.view"; a group Code like "group:catalog.products".</summary>
public sealed record CreatePermissionRequest(string Code, string Name, Guid? ParentId, bool IsGroup, int SortOrder);

public sealed record UpdatePermissionRequest(string Name, bool IsActive, Guid? ParentId, bool IsGroup, int SortOrder);

public sealed record PermissionResponse(
    Guid Id, string Code, string Name, bool IsActive, Guid? ParentId, bool IsGroup, int SortOrder);

/// <summary>An active node with its active children (groups nested to any depth), ordered by SortOrder.</summary>
public sealed record PermissionTreeNode(
    Guid Id, string Code, string Name, bool IsGroup, int SortOrder, IReadOnlyList<PermissionTreeNode> Children);

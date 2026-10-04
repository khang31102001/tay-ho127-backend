namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>A group node of the seeded permission tree. <paramref name="ParentCode"/> is null for a top-level
/// module group; parents must be listed before their children.</summary>
public sealed record PermissionGroupSeed(string Code, string Name, string? ParentCode, int SortOrder);

/// <summary>The seeded permission hierarchy: the group nodes, plus which group each leaf belongs to. A leaf's
/// resource is the part of its code before the first '.' (users.roles.manage → "users"); a resource with no
/// entry in <see cref="GroupCodeByResource"/> is placed in <see cref="FallbackGroupCode"/> so no permission is
/// ever left outside the tree.</summary>
public sealed record PermissionTreeSeed(
    IReadOnlyList<PermissionGroupSeed> Groups,
    IReadOnlyDictionary<string, string> GroupCodeByResource,
    string FallbackGroupCode);

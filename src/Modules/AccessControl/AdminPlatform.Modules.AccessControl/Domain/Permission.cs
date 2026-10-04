using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.AccessControl.Domain;

/// <summary>A node of the permission tree. A LEAF (<see cref="IsGroup"/> = false) is a real permission whose
/// Code (e.g. "users.view") is checked against JWT `permission` claims by
/// <see cref="AdminPlatform.Common.Security.PermissionAuthorizationHandler"/>. A GROUP (<see cref="IsGroup"/> =
/// true, Code "group:…") only organises leaves for the admin UI: it is never stored in role_permissions and
/// never put in a JWT, so authorization needs no recursion. Groups nest to any depth (see
/// <see cref="MaxDepth"/>); only groups can have children.</summary>
public sealed class Permission : CatalogEntity
{
    /// <summary>Deepest allowed chain, counting a root node as depth 1 (Module → Resource group → Leaf = 3).</summary>
    public const int MaxDepth = 5;

    public const string GroupCodePrefix = "group:";

    public Guid? ParentId { get; private set; }
    public bool IsGroup { get; private set; }
    public int SortOrder { get; private set; }

    private Permission()
    {
        // EF Core
    }

    public static Permission Create(string code, string name) => Create(code, name, null, 0);

    public static Permission Create(string code, string name, Guid? parentId, int sortOrder) =>
        Build(code, name, parentId, sortOrder, isGroup: false);

    public static Permission CreateGroup(string code, string name, Guid? parentId, int sortOrder) =>
        Build(code, name, parentId, sortOrder, isGroup: true);

    private static Permission Build(string code, string name, Guid? parentId, int sortOrder, bool isGroup)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            ParentId = parentId,
            IsGroup = isGroup,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
    }

    /// <summary>Places the node in the tree. Callers validate the parent/depth/cycle rules first.</summary>
    public void Place(Guid? parentId, int sortOrder)
    {
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    public void SetIsGroup(bool isGroup)
    {
        IsGroup = isGroup;
    }
}

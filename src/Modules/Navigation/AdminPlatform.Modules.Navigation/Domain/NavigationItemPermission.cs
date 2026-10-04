using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>Links an item to the permission CODE (not a Guid FK) that must be present in the caller's JWT
/// `permission` claims for the item to be visible — matches exactly what
/// <see cref="AdminPlatform.Common.Security.PermissionAuthorizationHandler"/> checks, so no cross-module lookup
/// against AccessControl is needed at read time. Codes are permission LEAVES, never `group:*` nodes.
/// An item with no rows is open to every caller of its scope (any signed-in admin; any website visitor).</summary>
public sealed class NavigationItemPermission : AuditableEntity
{
    public Guid ItemId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;

    private NavigationItemPermission()
    {
        // EF Core
    }

    public static NavigationItemPermission Create(Guid itemId, string permissionCode)
    {
        return new NavigationItemPermission
        {
            Id = Guid.NewGuid(),
            ItemId = Guard.NotEmpty(itemId, nameof(itemId)),
            PermissionCode = Guard.NotNullOrWhiteSpace(permissionCode, nameof(permissionCode)).Trim(),
        };
    }
}

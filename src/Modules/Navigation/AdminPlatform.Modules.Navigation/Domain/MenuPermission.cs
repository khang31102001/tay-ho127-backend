using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>Links a menu to the permission CODE (not a Guid FK) that must be present in the caller's JWT
/// `permission` claims for the menu to be visible — matches exactly what
/// <see cref="AdminPlatform.Common.Security.PermissionAuthorizationHandler"/> already checks, so no
/// cross-module lookup against AccessControl's Permission table is needed at read time (architecture
/// assumption #6). A menu with no rows here is visible to any authenticated user.</summary>
public sealed class MenuPermission : AuditableEntity
{
    public Guid MenuId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;

    private MenuPermission()
    {
        // EF Core
    }

    public static MenuPermission Create(Guid menuId, string permissionCode)
    {
        return new MenuPermission
        {
            Id = Guid.NewGuid(),
            MenuId = Guard.NotEmpty(menuId, nameof(menuId)),
            PermissionCode = Guard.NotNullOrWhiteSpace(permissionCode, nameof(permissionCode)).Trim(),
        };
    }
}

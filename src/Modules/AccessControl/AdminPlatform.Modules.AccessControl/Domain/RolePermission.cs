using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.AccessControl.Domain;

public sealed class RolePermission : AuditableEntity
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission()
    {
        // EF Core
    }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = Guard.NotEmpty(roleId, nameof(roleId)),
            PermissionId = Guard.NotEmpty(permissionId, nameof(permissionId)),
        };
    }
}

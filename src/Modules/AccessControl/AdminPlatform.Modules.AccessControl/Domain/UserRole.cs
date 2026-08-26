using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.AccessControl.Domain;

/// <summary>UserId is a plain Guid, not an EF navigation to Identity's User — cross-module references are
/// by id only (architecture assumption #6); a raw FK to identity.users(id) is added via migration SQL.</summary>
public sealed class UserRole : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRole()
    {
        // EF Core
    }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = Guard.NotEmpty(userId, nameof(userId)),
            RoleId = Guard.NotEmpty(roleId, nameof(roleId)),
        };
    }
}

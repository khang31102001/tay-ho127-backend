using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

/// <summary>UserId is a plain Guid, not an EF navigation to Identity's User (architecture assumption #6).</summary>
public sealed class UserDepartment : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid DepartmentId { get; private set; }

    private UserDepartment()
    {
        // EF Core
    }

    public static UserDepartment Create(Guid userId, Guid departmentId)
    {
        return new UserDepartment
        {
            Id = Guid.NewGuid(),
            UserId = Guard.NotEmpty(userId, nameof(userId)),
            DepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId)),
        };
    }
}

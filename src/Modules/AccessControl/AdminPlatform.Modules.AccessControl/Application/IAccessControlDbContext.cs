using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Application;

public interface IAccessControlDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

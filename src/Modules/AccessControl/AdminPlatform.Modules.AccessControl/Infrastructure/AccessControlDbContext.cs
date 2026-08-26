using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

public sealed class AccessControlDbContext : DbContext, IAccessControlDbContext
{
    public const string Schema = "access_control";

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessControlDbContext).Assembly);
    }
}

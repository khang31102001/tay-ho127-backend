using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>Idempotent seed: the full cross-module permission catalog, a SuperAdmin role granted every
/// permission, and (when an admin user id is supplied) that role assigned to the SuperAdmin account.
/// Everything is upserted by Code — safe to run on every deploy.</summary>
public static class AccessControlSeeder
{
    public const string SuperAdminRoleCode = "super-admin";

    public static async Task SeedAsync(
        IServiceProvider services,
        IReadOnlyCollection<(string Code, string Description)> permissionCatalog,
        Guid? superAdminUserId,
        CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAccessControlDbContext>();

        var existingPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);
        foreach (var (code, description) in permissionCatalog)
        {
            if (!existingPermissions.ContainsKey(code))
            {
                var permission = Permission.Create(code, description);
                db.Permissions.Add(permission);
                existingPermissions[code] = permission;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var superAdminRole = await db.Roles.SingleOrDefaultAsync(r => r.Code == SuperAdminRoleCode, cancellationToken);
        if (superAdminRole is null)
        {
            superAdminRole = Role.Create(SuperAdminRoleCode, "Super Admin");
            db.Roles.Add(superAdminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var grantedPermissionIds = await db.RolePermissions
            .Where(rp => rp.RoleId == superAdminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
        var grantedSet = grantedPermissionIds.ToHashSet();

        foreach (var permission in existingPermissions.Values)
        {
            if (!grantedSet.Contains(permission.Id))
            {
                db.RolePermissions.Add(RolePermission.Create(superAdminRole.Id, permission.Id));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (superAdminUserId is { } userId)
        {
            var alreadyAssigned = await db.UserRoles.AnyAsync(
                ur => ur.UserId == userId && ur.RoleId == superAdminRole.Id, cancellationToken);
            if (!alreadyAssigned)
            {
                db.UserRoles.Add(UserRole.Create(userId, superAdminRole.Id));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

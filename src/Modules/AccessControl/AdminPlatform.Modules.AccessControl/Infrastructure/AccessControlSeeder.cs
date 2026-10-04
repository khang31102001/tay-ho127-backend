using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>Idempotent seed: the full cross-module permission catalog, its group hierarchy, a SuperAdmin role
/// granted every permission LEAF, and (when an admin user id is supplied) that role assigned to the SuperAdmin
/// account. Everything is upserted by Code — safe to run on every deploy.
///
/// Hierarchy: groups are created/placed from <paramref name="tree"/>; each catalog leaf is (re)placed under the
/// group of its resource with a sort order that follows the catalog order. Only STRUCTURE (parent, sort order)
/// is re-synced for rows this seeder owns — a name an admin edited is never overwritten.</summary>
public static class AccessControlSeeder
{
    public const string SuperAdminRoleCode = "super-admin";

    public static async Task SeedAsync(
        IServiceProvider services,
        IReadOnlyCollection<(string Code, string Description)> permissionCatalog,
        Guid? superAdminUserId,
        CancellationToken cancellationToken,
        PermissionTreeSeed? tree = null)
    {
        var db = services.GetRequiredService<IAccessControlDbContext>();

        var existingPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        var groupIdByCode = new Dictionary<string, Guid>();
        if (tree is not null)
        {
            // Parents are listed before children, so one pass resolves every parent id; saved per group so a new
            // parent has its id persisted before a child references it.
            foreach (var group in tree.Groups)
            {
                Guid? parentId = group.ParentCode is null ? null : groupIdByCode[group.ParentCode];

                if (existingPermissions.TryGetValue(group.Code, out var existingGroup))
                {
                    existingGroup.Place(parentId, group.SortOrder);
                }
                else
                {
                    existingGroup = Permission.CreateGroup(group.Code, group.Name, parentId, group.SortOrder);
                    db.Permissions.Add(existingGroup);
                    existingPermissions[group.Code] = existingGroup;
                }

                groupIdByCode[group.Code] = existingGroup.Id;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        var sortOrder = 0;
        foreach (var (code, description) in permissionCatalog)
        {
            sortOrder++;
            Guid? parentId = null;
            if (tree is not null)
            {
                var resource = code.Split('.')[0];
                var groupCode = tree.GroupCodeByResource.TryGetValue(resource, out var mapped) ? mapped : tree.FallbackGroupCode;
                parentId = groupIdByCode[groupCode];
            }

            if (existingPermissions.TryGetValue(code, out var existing))
            {
                if (tree is not null)
                {
                    existing.Place(parentId, sortOrder);
                }
            }
            else
            {
                var permission = Permission.Create(code, description, parentId, sortOrder);
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

        // Groups are structure only — the role holds the leaves.
        foreach (var permission in existingPermissions.Values.Where(p => !p.IsGroup))
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

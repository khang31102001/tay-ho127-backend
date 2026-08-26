using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

internal sealed class RolePermissionQueryService : IRolePermissionQueryService
{
    private readonly IAccessControlDbContext _db;

    public RolePermissionQueryService(IAccessControlDbContext db)
    {
        _db = db;
    }

    public async Task<UserPermissionsSnapshot> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = _db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId);

        var roles = await _db.Roles
            .Where(r => roleIds.Contains(r.Id) && r.IsActive)
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);

        var activeRoleIds = _db.Roles.Where(r => roleIds.Contains(r.Id) && r.IsActive).Select(r => r.Id);
        var permissionIds = _db.RolePermissions.Where(rp => activeRoleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId);

        var permissions = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id) && p.IsActive)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new UserPermissionsSnapshot(roles, permissions);
    }
}

using AdminPlatform.Modules.AccessControl.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Application.UserRoles;

public sealed class UserRoleService : IUserRoleService
{
    private readonly IAccessControlDbContext _db;

    public UserRoleService(IAccessControlDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserRoleResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new UserRoleResponse(r.Id, r.Code, r.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignAsync(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        var alreadyAssigned = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == userId && ur.RoleId == request.RoleId, cancellationToken);
        if (alreadyAssigned)
        {
            return;
        }

        _db.UserRoles.Add(UserRole.Create(userId, request.RoleId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var assignment = await _db.UserRoles.SingleOrDefaultAsync(
            ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (assignment is null)
        {
            return;
        }

        _db.UserRoles.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

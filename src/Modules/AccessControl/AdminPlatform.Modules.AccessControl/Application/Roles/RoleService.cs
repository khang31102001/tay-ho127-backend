using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.AccessControl.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Application.Roles;

public sealed class RoleService : IRoleService
{
    private readonly IAccessControlDbContext _db;

    public RoleService(IAccessControlDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<RoleResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(r => EF.Functions.ILike(r.Code, pattern) || EF.Functions.ILike(r.Name, pattern));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "code" => request.IsDescending ? query.OrderByDescending(r => r.Code) : query.OrderBy(r => r.Code),
            _ => request.IsDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
        };

        var projected = query.Select(r => new RoleResponse(r.Id, r.Code, r.Name, r.IsActive, r.CreatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(role);
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Roles.AnyAsync(r => r.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A role with code '{request.Code}' already exists.");
        }

        var role = Role.Create(request.Code, request.Name);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(role);
    }

    public async Task<RoleResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await FindOrThrowAsync(id, cancellationToken);
        role.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await FindOrThrowAsync(id, cancellationToken);

        var inUse = await _db.UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("This role is still assigned to one or more users and cannot be deleted.");
        }

        var rolePermissions = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken);
        _db.RolePermissions.RemoveRange(rolePermissions);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPermissionIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        await FindOrThrowAsync(roleId, cancellationToken);
        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetPermissionsAsync(Guid roleId, AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        await FindOrThrowAsync(roleId, cancellationToken);

        var requested = request.PermissionIds.Distinct().ToHashSet();
        var validPermissionIds = await _db.Permissions
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (validPermissionIds.Count != requested.Count)
        {
            throw new BusinessRuleValidationException("One or more permission ids do not exist.");
        }

        var current = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        var currentIds = current.Select(rp => rp.PermissionId).ToHashSet();

        var toRemove = current.Where(rp => !requested.Contains(rp.PermissionId));
        _db.RolePermissions.RemoveRange(toRemove);

        foreach (var permissionId in requested.Except(currentIds))
        {
            _db.RolePermissions.Add(RolePermission.Create(roleId, permissionId));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), id);

    private static RoleResponse ToResponse(Role role) => new(role.Id, role.Code, role.Name, role.IsActive, role.CreatedAtUtc);
}

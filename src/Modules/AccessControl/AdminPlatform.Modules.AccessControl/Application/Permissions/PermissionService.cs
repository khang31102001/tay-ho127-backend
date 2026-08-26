using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.AccessControl.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IAccessControlDbContext _db;

    public PermissionService(IAccessControlDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PermissionResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, pattern) || EF.Functions.ILike(p.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code);

        var projected = query.Select(p => new PermissionResponse(p.Id, p.Code, p.Name, p.IsActive));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PermissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(permission);
    }

    public async Task<PermissionResponse> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Permissions.AnyAsync(p => p.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A permission with code '{request.Code}' already exists.");
        }

        var permission = Permission.Create(request.Code, request.Name);
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(permission);
    }

    public async Task<PermissionResponse> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await FindOrThrowAsync(id, cancellationToken);
        permission.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(permission);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await FindOrThrowAsync(id, cancellationToken);

        var inUse = await _db.RolePermissions.AnyAsync(rp => rp.PermissionId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("This permission is still assigned to one or more roles and cannot be deleted.");
        }

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Permission> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Permissions.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Permission), id);

    private static PermissionResponse ToResponse(Permission permission) =>
        new(permission.Id, permission.Code, permission.Name, permission.IsActive);
}

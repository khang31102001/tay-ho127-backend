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

        var projected = query.Select(p => new PermissionResponse(p.Id, p.Code, p.Name, p.IsActive, p.ParentId, p.IsGroup, p.SortOrder));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionTreeNode>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var all = await _db.Permissions.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Code, p.Name, p.ParentId, p.IsGroup, p.SortOrder })
            .ToListAsync(cancellationToken);

        var activeIds = all.Select(p => p.Id).ToHashSet();
        var childrenByParent = all.Where(p => p.ParentId is not null && activeIds.Contains(p.ParentId.Value))
            .ToLookup(p => p.ParentId!.Value);

        // A node whose parent is missing/inactive is NOT promoted to the root: switching a group off hides its subtree.
        var roots = all.Where(p => p.ParentId is null);

        var depthGuard = Permission.MaxDepth + 1;
        List<PermissionTreeNode> Build(IEnumerable<(Guid Id, string Code, string Name, bool IsGroup, int SortOrder)> nodes, int depth) =>
            depth > depthGuard
                ? []
                : nodes.OrderBy(n => n.SortOrder).ThenBy(n => n.Name)
                    .Select(n => new PermissionTreeNode(
                        n.Id, n.Code, n.Name, n.IsGroup, n.SortOrder,
                        n.IsGroup
                            ? Build(childrenByParent[n.Id].Select(c => (c.Id, c.Code, c.Name, c.IsGroup, c.SortOrder)), depth + 1)
                            : []))
                    .ToList();

        return Build(roots.Select(p => (p.Id, p.Code, p.Name, p.IsGroup, p.SortOrder)), 1);
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

        var nodes = await LoadNodesAsync(cancellationToken);
        EnsureParentAllowsChild(request.ParentId, nodes, subtreeHeight: 1);

        var permission = request.IsGroup
            ? Permission.CreateGroup(request.Code, request.Name, request.ParentId, request.SortOrder)
            : Permission.Create(request.Code, request.Name, request.ParentId, request.SortOrder);
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(permission);
    }

    public async Task<PermissionResponse> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await FindOrThrowAsync(id, cancellationToken);
        var nodes = await LoadNodesAsync(cancellationToken);
        var childrenByParent = nodes.Values.Where(n => n.ParentId is not null).ToLookup(n => n.ParentId!.Value);

        if (request.IsGroup != permission.IsGroup)
        {
            if (request.IsGroup)
            {
                var granted = await _db.RolePermissions.AnyAsync(rp => rp.PermissionId == id, cancellationToken);
                if (granted)
                {
                    throw new BusinessRuleValidationException(
                        "This permission is assigned to roles, so it cannot become a group. Remove it from the roles first.");
                }
            }
            else if (childrenByParent[id].Any())
            {
                throw new BusinessRuleValidationException("A group that still has children cannot become a permission.");
            }
        }

        if (request.ParentId is { } parentId)
        {
            if (parentId == id || PermissionTreeRules.IsSelfOrDescendant(parentId, id, nodes))
            {
                throw new BusinessRuleValidationException("A permission cannot be moved under itself or one of its descendants.");
            }
        }

        EnsureParentAllowsChild(request.ParentId, nodes, PermissionTreeRules.SubtreeHeight(id, childrenByParent));

        permission.Update(request.Name, request.IsActive);
        permission.SetIsGroup(request.IsGroup);
        permission.Place(request.ParentId, request.SortOrder);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(permission);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await FindOrThrowAsync(id, cancellationToken);

        var hasChildren = await _db.Permissions.AnyAsync(p => p.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new ConflictException("This group still has children and cannot be deleted.");
        }

        var inUse = await _db.RolePermissions.AnyAsync(rp => rp.PermissionId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("This permission is still assigned to one or more roles and cannot be deleted.");
        }

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureParentAllowsChild(
        Guid? parentId, IReadOnlyDictionary<Guid, PermissionNodeInfo> nodes, int subtreeHeight)
    {
        if (parentId is not { } id)
        {
            return;
        }

        if (!nodes.TryGetValue(id, out var parent))
        {
            throw new BusinessRuleValidationException("The parent permission does not exist.");
        }

        if (!parent.IsGroup)
        {
            throw new BusinessRuleValidationException("The parent must be a group.");
        }

        if (PermissionTreeRules.Depth(id, nodes) + subtreeHeight > Permission.MaxDepth)
        {
            throw new BusinessRuleValidationException($"The permission tree can be at most {Permission.MaxDepth} levels deep.");
        }
    }

    private async Task<Dictionary<Guid, PermissionNodeInfo>> LoadNodesAsync(CancellationToken cancellationToken) =>
        await _db.Permissions.AsNoTracking()
            .Select(p => new PermissionNodeInfo(p.Id, p.ParentId, p.IsGroup, p.IsActive))
            .ToDictionaryAsync(n => n.Id, cancellationToken);

    private async Task<Permission> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Permissions.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Permission), id);

    private static PermissionResponse ToResponse(Permission permission) =>
        new(permission.Id, permission.Code, permission.Name, permission.IsActive, permission.ParentId, permission.IsGroup, permission.SortOrder);
}

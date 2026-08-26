using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Organization.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application.Departments;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IOrganizationDbContext _db;

    public DepartmentService(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<DepartmentResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken)
    {
        var query = _db.Departments.AsNoTracking().AsQueryable();

        if (organizationId is { } orgId)
        {
            query = query.Where(d => d.OrganizationId == orgId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(d => EF.Functions.ILike(d.Code, pattern) || EF.Functions.ILike(d.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name);

        var projected = query.Select(d => new DepartmentResponse(d.Id, d.OrganizationId, d.Code, d.Name, d.IsActive, d.ParentId, d.CreatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(department);
    }

    public async Task<IReadOnlyList<DepartmentTreeNode>> GetTreeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return BuildTree(departments, null);
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Departments.AnyAsync(
            d => d.OrganizationId == request.OrganizationId && d.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A department with code '{request.Code}' already exists in this organization.");
        }

        if (request.ParentId is { } parentId)
        {
            var parentExists = await _db.Departments.AnyAsync(
                d => d.Id == parentId && d.OrganizationId == request.OrganizationId, cancellationToken);
            if (!parentExists)
            {
                throw new BusinessRuleValidationException("Parent department must belong to the same organization.");
            }
        }

        var department = Department.Create(request.OrganizationId, request.Code, request.Name, request.ParentId);
        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(department);
    }

    public async Task<DepartmentResponse> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await FindOrThrowAsync(id, cancellationToken);

        if (request.ParentId is { } parentId)
        {
            if (parentId == id)
            {
                throw new BusinessRuleValidationException("A department cannot be its own parent.");
            }

            var descendantIds = await GetDescendantIdsAsync(id, department.OrganizationId, cancellationToken);
            if (descendantIds.Contains(parentId))
            {
                throw new BusinessRuleValidationException("Cannot move a department under its own descendant.");
            }

            var parentExists = await _db.Departments.AnyAsync(
                d => d.Id == parentId && d.OrganizationId == department.OrganizationId, cancellationToken);
            if (!parentExists)
            {
                throw new BusinessRuleValidationException("Parent department must belong to the same organization.");
            }
        }

        department.Update(request.Name, request.IsActive, request.ParentId);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(department);
    }

    private async Task<HashSet<Guid>> GetDescendantIdsAsync(Guid rootId, Guid organizationId, CancellationToken cancellationToken)
    {
        var all = await _db.Departments
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId)
            .Select(d => new { d.Id, d.ParentId })
            .ToListAsync(cancellationToken);

        var childrenByParent = all
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (result.Add(child))
                {
                    stack.Push(child);
                }
            }
        }

        return result;
    }

    private static List<DepartmentTreeNode> BuildTree(List<Department> all, Guid? parentId)
    {
        return all
            .Where(d => d.ParentId == parentId)
            .Select(d => new DepartmentTreeNode(d.Id, d.Code, d.Name, d.IsActive, BuildTree(all, d.Id)))
            .ToList();
    }

    private async Task<Department> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Departments.SingleOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), id);

    private static DepartmentResponse ToResponse(Department department) => new(
        department.Id, department.OrganizationId, department.Code, department.Name, department.IsActive, department.ParentId, department.CreatedAtUtc);
}

using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Organization.Application.Departments;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken);

    Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentTreeNode>> GetTreeAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken);

    Task<DepartmentResponse> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken);
}

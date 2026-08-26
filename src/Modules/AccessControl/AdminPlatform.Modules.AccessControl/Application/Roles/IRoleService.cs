using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.AccessControl.Application.Roles;

public interface IRoleService
{
    Task<PagedResult<RoleResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task<RoleResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetPermissionIdsAsync(Guid roleId, CancellationToken cancellationToken);

    Task SetPermissionsAsync(Guid roleId, AssignPermissionsRequest request, CancellationToken cancellationToken);
}

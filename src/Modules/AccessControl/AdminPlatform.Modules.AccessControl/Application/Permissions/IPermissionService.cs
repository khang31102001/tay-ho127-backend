using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

public interface IPermissionService
{
    Task<PagedResult<PermissionResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    /// <summary>The active permission tree (groups nested to any depth) for the role/menu pickers.</summary>
    Task<IReadOnlyList<PermissionTreeNode>> GetTreeAsync(CancellationToken cancellationToken);

    Task<PermissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PermissionResponse> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken);

    Task<PermissionResponse> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

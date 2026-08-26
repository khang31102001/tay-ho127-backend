using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Navigation.Application.Menus;

public interface IMenuService
{
    Task<PagedResult<MenuResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<MenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuTreeNode>> GetTreeAsync(CancellationToken cancellationToken);

    Task<MenuResponse> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken);

    Task<MenuResponse> UpdateAsync(Guid id, UpdateMenuRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid menuId, CancellationToken cancellationToken);

    Task SetPermissionsAsync(Guid menuId, AssignMenuPermissionsRequest request, CancellationToken cancellationToken);
}

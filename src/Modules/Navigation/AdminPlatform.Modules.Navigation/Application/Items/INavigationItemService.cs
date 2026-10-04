using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Navigation.Application.Items;

public interface INavigationItemService
{
    /// <summary>Items of the menus the caller may view (all scopes they have a view permission for), or of one menu.</summary>
    Task<PagedResult<NavigationItemResponse>> ListAsync(Guid? menuId, PagedRequest request, CancellationToken cancellationToken);

    Task<NavigationItemResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<NavigationItemResponse> CreateAsync(CreateNavigationItemRequest request, CancellationToken cancellationToken);

    Task<NavigationItemResponse> UpdateAsync(Guid id, UpdateNavigationItemRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task ReorderAsync(ReorderNavigationItemsRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid id, CancellationToken cancellationToken);

    Task SetPermissionsAsync(Guid id, AssignNavigationItemPermissionsRequest request, CancellationToken cancellationToken);
}

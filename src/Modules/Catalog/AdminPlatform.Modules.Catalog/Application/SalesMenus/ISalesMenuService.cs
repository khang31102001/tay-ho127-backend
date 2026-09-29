using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenus;

public interface ISalesMenuService
{
    Task<PagedResult<SalesMenuResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken);

    Task<SalesMenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SalesMenuResponse> CreateAsync(CreateSalesMenuRequest request, CancellationToken cancellationToken);

    Task<SalesMenuResponse> UpdateAsync(Guid id, UpdateSalesMenuRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

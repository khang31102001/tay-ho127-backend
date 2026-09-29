using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;

public interface ISalesMenuProductService
{
    Task<PagedResult<SalesMenuProductResponse>> ListAsync(PagedRequest request, Guid? salesMenuId, Guid? productId, CancellationToken cancellationToken);

    Task<SalesMenuProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SalesMenuProductResponse> CreateAsync(CreateSalesMenuProductRequest request, CancellationToken cancellationToken);

    Task<SalesMenuProductResponse> UpdateAsync(Guid id, UpdateSalesMenuProductRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

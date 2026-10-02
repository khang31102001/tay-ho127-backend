using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> ListAsync(PagedRequest request, Guid? categoryId, bool? isActive, CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

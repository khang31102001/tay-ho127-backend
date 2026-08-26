using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Organization.Application.Brands;

public interface IBrandService
{
    Task<PagedResult<BrandResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken);

    Task<BrandResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BrandResponse> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken);

    Task<BrandResponse> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken);
}

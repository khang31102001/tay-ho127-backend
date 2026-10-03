using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.Banners;

public interface IBannerService
{
    /// <param name="placement">Filters by placement wire name (e.g. "HOME_HERO").</param>
    Task<PagedResult<BannerResponse>> ListAsync(PagedRequest request, string? placement, bool? isActive, CancellationToken cancellationToken);

    Task<BannerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BannerResponse> CreateAsync(CreateBannerRequest request, CancellationToken cancellationToken);

    Task<BannerResponse> UpdateAsync(Guid id, UpdateBannerRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

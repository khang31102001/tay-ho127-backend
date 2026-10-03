using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

public interface IPromotionService
{
    /// <param name="status">Filters by the stored status ("draft" | "active" | "inactive").</param>
    Task<PagedResult<PromotionResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken);

    Task<PromotionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PromotionResponse> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken);

    Task<PromotionResponse> UpdateAsync(Guid id, UpdatePromotionRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

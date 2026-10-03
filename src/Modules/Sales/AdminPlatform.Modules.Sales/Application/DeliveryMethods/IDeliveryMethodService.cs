using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Sales.Application.DeliveryMethods;

public interface IDeliveryMethodService
{
    /// <param name="isActive">Filters by the active flag; Search matches the code or the name.</param>
    Task<PagedResult<DeliveryMethodResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken);

    Task<DeliveryMethodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>409 when the code is already used.</summary>
    Task<DeliveryMethodResponse> CreateAsync(CreateDeliveryMethodRequest request, CancellationToken cancellationToken);

    Task<DeliveryMethodResponse> UpdateAsync(Guid id, UpdateDeliveryMethodRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Active methods in display order, for the checkout (anonymous).</summary>
    Task<IReadOnlyList<PublicDeliveryMethodResponse>> ListPublicAsync(CancellationToken cancellationToken);
}

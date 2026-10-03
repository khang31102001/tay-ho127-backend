using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Sales.Application.PaymentMethods;

public interface IPaymentMethodService
{
    /// <param name="isActive">Filters by the active flag; Search matches the code or the name.</param>
    Task<PagedResult<PaymentMethodResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken);

    Task<PaymentMethodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>409 when the code is already used.</summary>
    Task<PaymentMethodResponse> CreateAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken);

    Task<PaymentMethodResponse> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Active methods in display order, for the checkout (anonymous).</summary>
    Task<IReadOnlyList<PublicPaymentMethodResponse>> ListPublicAsync(CancellationToken cancellationToken);
}

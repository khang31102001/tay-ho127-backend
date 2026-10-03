using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Application.Orders;

namespace AdminPlatform.Modules.Sales.Application.PaymentSessions;

public interface IPaymentSessionService
{
    /// <summary>Reserves an order that is paid up front (QR, wallet). Same body and rules as placing an order, but
    /// nothing is created yet and the discount code is not used up: that happens when staff confirm the money.
    /// A repeated IdempotencyKey returns the session created the first time. A cash-on-delivery method is refused
    /// (use the order endpoint).</summary>
    Task<PaymentSessionResponse> CreateAsync(CreateOrderRequest request, Guid? customerId, CancellationToken cancellationToken);

    /// <summary>Public read by the (unguessable) session id. A pending session past its deadline is cancelled here.</summary>
    Task<PaymentSessionResponse> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Admin list, newest first. Search matches the reference code, customer name or phone.</summary>
    Task<PagedResult<PaymentSessionResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken);

    /// <summary>The customer gives up: pending/failed → cancelled. 400 once an order was created.</summary>
    Task<PaymentSessionResponse> CancelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>After a failure the customer asks to try again: failed → pending with a new deadline.</summary>
    Task<PaymentSessionResponse> RetryAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Staff confirm the money arrived: re-prices the request, creates the order with a PAID payment and
    /// closes the session — one step. 409 when the session is not pending, the price changed since it was quoted,
    /// or the discount code ran out.</summary>
    Task<PaymentSessionResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Staff could not find the money: pending → failed, with a note the customer sees.</summary>
    Task<PaymentSessionResponse> RejectAsync(Guid id, RejectPaymentSessionRequest request, CancellationToken cancellationToken);
}

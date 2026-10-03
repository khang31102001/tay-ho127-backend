using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Sales.Application.Orders;

public interface IOrderService
{
    /// <summary>Places a cash-on-delivery order (anonymous or signed-in customer). Prices everything on the server;
    /// 400 for an unsellable dish, a bad option, an unavailable method or a refused discount code; 409 when the
    /// code's usage limit is reached. A repeated IdempotencyKey returns the order placed the first time.
    /// A method that is paid up front is refused here — it goes through a payment session.</summary>
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? customerId, CancellationToken cancellationToken);

    /// <summary>Admin list, newest first. Search matches the order code, customer name or phone.</summary>
    Task<PagedResult<OrderResponse>> ListAsync(PagedRequest request, string? status, string? paymentStatus, CancellationToken cancellationToken);

    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Moves the order along the state machine (400 for an illegal move) and records who did it.
    /// Cancelling also cancels a still-unpaid payment and gives the discount code's use back; an already paid
    /// payment is left for staff to refund by hand.</summary>
    Task<OrderResponse> ChangeStatusAsync(Guid id, ChangeOrderStatusRequest request, CancellationToken cancellationToken);

    /// <summary>A guest's view of their own order. 404 unless BOTH the code and the phone match.</summary>
    Task<OrderResponse> LookupAsync(OrderLookupRequest request, CancellationToken cancellationToken);

    /// <summary>A failed payment back to pending so it can be paid again (guest, code + phone).</summary>
    Task<OrderResponse> RetryPaymentAsync(OrderLookupRequest request, CancellationToken cancellationToken);

    /// <summary>The signed-in customer's own orders, newest first.</summary>
    Task<PagedResult<OrderResponse>> ListForCustomerAsync(Guid customerId, PagedRequest request, CancellationToken cancellationToken);

    /// <summary>One of the signed-in customer's own orders by code; 404 for anyone else's.</summary>
    Task<OrderResponse> GetForCustomerAsync(Guid customerId, string orderCode, CancellationToken cancellationToken);
}

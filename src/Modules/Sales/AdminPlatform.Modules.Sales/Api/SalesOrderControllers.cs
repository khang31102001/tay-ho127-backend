using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.Payments;
using AdminPlatform.Modules.Sales.Application.PaymentSessions;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Sales.Api;

[ApiController]
[Route("api/v1/sales/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Newest first. status / paymentStatus filter by wire value ("pending", "paid"...); search matches
    /// the order code, customer name or phone.</summary>
    [HttpGet]
    [RequirePermission(SalesPermissions.OrdersView)]
    [ProducesResponseType<PagedResult<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, [FromQuery] string? paymentStatus, CancellationToken cancellationToken) =>
        Ok(await _orderService.ListAsync(request, status, paymentStatus, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(SalesPermissions.OrdersView)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _orderService.GetByIdAsync(id, cancellationToken));

    /// <summary>400 for a move the order state machine does not allow; 403 when cancelling without orders.cancel.</summary>
    [HttpPost("{id:guid}/status")]
    [RequirePermission(SalesPermissions.OrdersUpdateStatus)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> ChangeStatus(Guid id, [FromBody] ChangeOrderStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await _orderService.ChangeStatusAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/v1/sales/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.PaymentsView)]
    [ProducesResponseType<PagedResult<PaymentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await _paymentService.ListAsync(request, status, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(SalesPermissions.PaymentsView)]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _paymentService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/transactions")]
    [RequirePermission(SalesPermissions.PaymentsView)]
    [ProducesResponseType<IReadOnlyList<PaymentTransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentTransactionResponse>>> ListTransactions(Guid id, CancellationToken cancellationToken) =>
        Ok(await _paymentService.ListTransactionsAsync(id, cancellationToken));

    /// <summary>400 for a move the payment state machine does not allow.</summary>
    [HttpPost("{id:guid}/transition")]
    [RequirePermission(SalesPermissions.PaymentsManage)]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> Transition(Guid id, [FromBody] TransitionPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await _paymentService.TransitionAsync(id, request, cancellationToken));
}

/// <summary>Staff side of the QR / wallet flow: payment sessions waiting for the money to be confirmed.</summary>
[ApiController]
[Route("api/v1/sales/payment-sessions")]
public sealed class PaymentSessionsController : ControllerBase
{
    private readonly IPaymentSessionService _sessionService;

    public PaymentSessionsController(IPaymentSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.PaymentsView)]
    [ProducesResponseType<PagedResult<PaymentSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentSessionResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await _sessionService.ListAsync(request, status, cancellationToken));

    /// <summary>The money arrived: creates the order with a paid payment and closes the session. 409 when the session is
    /// no longer pending, its price changed since it was quoted, or the discount code ran out.</summary>
    [HttpPost("{id:guid}/confirm")]
    [RequirePermission(SalesPermissions.PaymentsManage)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSessionResponse>> Confirm(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sessionService.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(SalesPermissions.PaymentsManage)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSessionResponse>> Reject(Guid id, [FromBody] RejectPaymentSessionRequest request, CancellationToken cancellationToken) =>
        Ok(await _sessionService.RejectAsync(id, request, cancellationToken));
}

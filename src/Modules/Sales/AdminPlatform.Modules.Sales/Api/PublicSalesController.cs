using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Sales.Application.DeliveryMethods;
using AdminPlatform.Modules.Sales.Application.OrderOptions;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.PaymentMethods;
using AdminPlatform.Modules.Sales.Application.PaymentSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdminPlatform.Modules.Sales.Api;

/// <summary>What the website's checkout and tracking pages need, without an admin login. Ordering works for guests;
/// a signed-in customer's token (if sent) only links the order to their account. Everything that costs money or
/// reveals personal data is rate-limited.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/sales/public")]
public sealed class PublicSalesController : ControllerBase
{
    /// <summary>Rate-limiter policy (registered by the Host) for placing an order / opening a payment session.</summary>
    public const string OrderRateLimitPolicy = "sales-order";

    /// <summary>Rate-limiter policy (registered by the Host) for the guest endpoints that take an order code + phone.</summary>
    public const string LookupRateLimitPolicy = "sales-lookup";

    private readonly IDeliveryMethodService _deliveryMethods;
    private readonly IPaymentMethodService _paymentMethods;
    private readonly IOrderOptionService _orderOptions;
    private readonly IOrderService _orders;
    private readonly IPaymentSessionService _sessions;

    public PublicSalesController(
        IDeliveryMethodService deliveryMethods,
        IPaymentMethodService paymentMethods,
        IOrderOptionService orderOptions,
        IOrderService orders,
        IPaymentSessionService sessions)
    {
        _deliveryMethods = deliveryMethods;
        _paymentMethods = paymentMethods;
        _orderOptions = orderOptions;
        _orders = orders;
        _sessions = sessions;
    }

    /// <summary>Only the customer realm's token links the order to an account; an admin token never does.</summary>
    private Guid? CurrentCustomerId =>
        User.Identity?.IsAuthenticated == true && User.HasClaim(AppClaimTypes.AccountType, AccountTypes.Customer)
            ? User.GetUserId()
            : null;

    [HttpGet("delivery-methods")]
    [ProducesResponseType<IReadOnlyList<PublicDeliveryMethodResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicDeliveryMethodResponse>>> ListDeliveryMethods(CancellationToken cancellationToken) =>
        Ok(await _deliveryMethods.ListPublicAsync(cancellationToken));

    [HttpGet("payment-methods")]
    [ProducesResponseType<IReadOnlyList<PublicPaymentMethodResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicPaymentMethodResponse>>> ListPaymentMethods(CancellationToken cancellationToken) =>
        Ok(await _paymentMethods.ListPublicAsync(cancellationToken));

    [HttpGet("order-options")]
    [ProducesResponseType<IReadOnlyList<OrderOptionGroupResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderOptionGroupResponse>>> ListOrderOptions(CancellationToken cancellationToken) =>
        Ok(await _orderOptions.ListPublicAsync(cancellationToken));

    /// <summary>Places a cash-on-delivery order. 201 with the order (also for a repeated IdempotencyKey, which returns
    /// the first order). 400 for anything the server refuses to sell; 409 when the discount code ran out.</summary>
    [HttpPost("orders")]
    [EnableRateLimiting(OrderRateLimitPolicy)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _orders.CreateAsync(request, CurrentCustomerId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, order);
    }

    /// <summary>POST (not GET) so the phone number never lands in a URL or a log. 404 unless both match.</summary>
    [HttpPost("orders/lookup")]
    [EnableRateLimiting(LookupRateLimitPolicy)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> LookupOrder([FromBody] OrderLookupRequest request, CancellationToken cancellationToken) =>
        Ok(await _orders.LookupAsync(request, cancellationToken));

    /// <summary>Lets the guest pay again after a failed payment (failed → pending).</summary>
    [HttpPost("orders/retry-payment")]
    [EnableRateLimiting(LookupRateLimitPolicy)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> RetryPayment([FromBody] OrderLookupRequest request, CancellationToken cancellationToken) =>
        Ok(await _orders.RetryPaymentAsync(request, cancellationToken));

    /// <summary>Opens a payment session for a QR / wallet order: nothing is created until staff confirm the money.</summary>
    [HttpPost("payment-sessions")]
    [EnableRateLimiting(OrderRateLimitPolicy)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentSessionResponse>> CreatePaymentSession([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var session = await _sessions.CreateAsync(request, CurrentCustomerId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, session);
    }

    /// <summary>The session id is unguessable and acts as the key; polled by the payment page until it succeeds.</summary>
    [HttpGet("payment-sessions/{id:guid}")]
    [EnableRateLimiting(LookupRateLimitPolicy)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSessionResponse>> GetPaymentSession(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sessions.GetAsync(id, cancellationToken));

    [HttpPost("payment-sessions/{id:guid}/cancel")]
    [EnableRateLimiting(LookupRateLimitPolicy)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSessionResponse>> CancelPaymentSession(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sessions.CancelAsync(id, cancellationToken));

    [HttpPost("payment-sessions/{id:guid}/retry")]
    [EnableRateLimiting(LookupRateLimitPolicy)]
    [ProducesResponseType<PaymentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSessionResponse>> RetryPaymentSession(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sessions.RetryAsync(id, cancellationToken));
}

/// <summary>The signed-in customer's own order history (customer realm only).</summary>
[ApiController]
[RequireAccountType(AccountTypes.Customer)]
[Route("api/v1/sales/customer/orders")]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public CustomerOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _orders.ListForCustomerAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>404 for an order that belongs to someone else (or to a guest).</summary>
    [HttpGet("{orderCode}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetByCode(string orderCode, CancellationToken cancellationToken) =>
        Ok(await _orders.GetForCustomerAsync(User.GetUserId(), orderCode, cancellationToken));
}

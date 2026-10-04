using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdminPlatform.Modules.Sales.Application.Orders;

internal sealed class OrderService : IOrderService
{
    private readonly ISalesDbContext _db;
    private readonly OrderPricer _pricer;
    private readonly OrderPlacer _placer;
    private readonly ICustomerDirectory _customers;
    private readonly IPromotionPricing _promotions;
    private readonly IOrderNotifier _notifier;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ISalesDbContext db,
        OrderPricer pricer,
        OrderPlacer placer,
        ICustomerDirectory customers,
        IPromotionPricing promotions,
        IOrderNotifier notifier,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILogger<OrderService> logger)
    {
        _db = db;
        _pricer = pricer;
        _placer = placer;
        _customers = customers;
        _promotions = promotions;
        _notifier = notifier;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? customerId, CancellationToken cancellationToken)
    {
        if (await _placer.FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken) is { } replay)
        {
            return OrderMapper.ToResponse(replay.Order, replay.Payment.Id);
        }

        var priced = await _pricer.PriceAsync(request, cancellationToken);
        if (!priced.PaymentMethod.IsPaidOnDelivery)
        {
            throw new BusinessRuleValidationException("Phương thức thanh toán này cần thanh toán trước. Vui lòng tạo phiên thanh toán.");
        }

        // A customer token whose account vanished (deleted) still places the order — as a guest.
        var verifiedCustomerId = customerId is { } id && await _customers.ExistsAsync(id, cancellationToken) ? customerId : null;

        var placed = await _placer.PlaceAsync(
            request,
            priced,
            new PlaceOrderOptions(verifiedCustomerId, request.IdempotencyKey, AlreadyPaid: false, OrderPlacer.CustomerActor, null, null),
            cancellationToken);
        return OrderMapper.ToResponse(placed.Order, placed.Payment.Id);
    }

    public async Task<PagedResult<OrderResponse>> ListAsync(PagedRequest request, string? status, string? paymentStatus, CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = EnumWire.Parse<OrderStatus>(status, "order status");
            query = query.Where(o => o.OrderStatus == parsed);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            var parsed = EnumWire.Parse<PaymentStatus>(paymentStatus, "payment status");
            query = query.Where(o => o.PaymentStatus == parsed);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(o => EF.Functions.ILike(o.OrderCode, pattern)
                || EF.Functions.ILike(o.CustomerName, pattern)
                || EF.Functions.ILike(o.Phone, pattern));
        }

        query = request.IsDescending
            ? query.OrderBy(o => o.CreatedAtUtc)
            : query.OrderByDescending(o => o.CreatedAtUtc);

        return await ToPagedResponsesAsync(query, request, cancellationToken);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.AsNoTracking().WithDetails().SingleOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), id);
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> ChangeStatusAsync(Guid id, ChangeOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var to = EnumWire.Parse<OrderStatus>(request.ToStatus, "order status");
        if (to == OrderStatus.Cancelled && !_currentUser.HasPermission(SalesPermissionCodes.OrdersCancel))
        {
            throw new ForbiddenException("Bạn không có quyền hủy đơn hàng.");
        }

        var order = await _db.Orders.WithDetails().SingleOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), id);

        var from = order.OrderStatus;
        var now = _clock.UtcNow;
        var actor = _currentUser.Email ?? "Quản trị viên";
        var note = request.Note;

        var payment = await _db.Payments.Include(p => p.Transactions).SingleOrDefaultAsync(p => p.OrderId == id, cancellationToken);
        if (to == OrderStatus.Cancelled && payment is not null)
        {
            if (payment.Status is PaymentStatus.Pending or PaymentStatus.Failed)
            {
                payment.Transition(PaymentStatus.Cancelled, now, actor, "Đơn hàng đã bị hủy.");
                order.SyncPaymentStatus(payment.Status);
            }
            else if (payment.Status == PaymentStatus.Paid)
            {
                note = string.IsNullOrWhiteSpace(note) ? "Đơn đã thanh toán — cần hoàn tiền thủ công." : $"{note.Trim()} (Đơn đã thanh toán — cần hoàn tiền thủ công.)";
            }
        }

        order.ChangeStatus(to, now, actor, _currentUser.UserId, note);
        await _db.SaveChangesAsync(cancellationToken);

        if (to == OrderStatus.Cancelled && order.PromotionId is { } promotionId)
        {
            await GiveBackPromotionUseAsync(promotionId, order.OrderCode);
        }

        await NotifyStatusChangedAsync(order, from, to, cancellationToken);
        return OrderMapper.ToResponse(order, payment?.Id);
    }

    public async Task<OrderResponse> LookupAsync(OrderLookupRequest request, CancellationToken cancellationToken)
    {
        var order = await FindForGuestAsync(request, tracked: false, cancellationToken);
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> RetryPaymentAsync(OrderLookupRequest request, CancellationToken cancellationToken)
    {
        var order = await FindForGuestAsync(request, tracked: true, cancellationToken);
        var payment = await _db.Payments.Include(p => p.Transactions).SingleOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), order.Id);

        if (payment.Status != PaymentStatus.Failed || order.OrderStatus == OrderStatus.Cancelled)
        {
            throw new BusinessRuleValidationException("Chỉ có thể thanh toán lại khi giao dịch thất bại và đơn hàng chưa bị hủy.");
        }

        payment.Transition(PaymentStatus.Pending, _clock.UtcNow, OrderPlacer.CustomerActor, "Khách hàng yêu cầu thanh toán lại.");
        order.SyncPaymentStatus(payment.Status);
        await _db.SaveChangesAsync(cancellationToken);
        return OrderMapper.ToResponse(order, payment.Id);
    }

    public async Task<PagedResult<OrderResponse>> ListForCustomerAsync(Guid customerId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc);
        return await ToPagedResponsesAsync(query, request, cancellationToken);
    }

    public async Task<OrderResponse> GetForCustomerAsync(Guid customerId, string orderCode, CancellationToken cancellationToken)
    {
        var code = (orderCode ?? string.Empty).Trim();
        var order = await _db.Orders.AsNoTracking().WithDetails()
            .SingleOrDefaultAsync(o => o.OrderCode == code && o.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), code);
        return await ToResponseAsync(order, cancellationToken);
    }

    /// <summary>Guests prove ownership with the order code AND the phone. Every mismatch is the same 404, so the
    /// endpoint cannot be used to find out which codes exist.</summary>
    private async Task<Order> FindForGuestAsync(OrderLookupRequest request, bool tracked, CancellationToken cancellationToken)
    {
        var code = (request.OrderCode ?? string.Empty).Trim();
        var notFound = new NotFoundException(nameof(Order), code);
        if (code.Length == 0 || !PhoneNumber.TryNormalize(request.Phone, out var phone))
        {
            throw notFound;
        }

        var query = tracked ? _db.Orders.AsQueryable() : _db.Orders.AsNoTracking();
        var order = await query.WithDetails().SingleOrDefaultAsync(o => o.OrderCode == code, cancellationToken);
        return order is not null && order.Phone == phone ? order : throw notFound;
    }

    private async Task<PagedResult<OrderResponse>> ToPagedResponsesAsync(IQueryable<Order> query, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await query.WithDetails().ToPagedResultAsync(request, cancellationToken);
        var paymentIds = await OrderMapper.LoadPaymentIdsAsync(_db, page.Items.Select(o => o.Id).ToList(), cancellationToken);
        var items = page.Items.Select(o => OrderMapper.ToResponse(o, paymentIds.GetValueOrDefault(o.Id))).ToList();
        return new PagedResult<OrderResponse>(items, page.Page, page.PageSize, page.TotalItems);
    }

    private async Task<OrderResponse> ToResponseAsync(Order order, CancellationToken cancellationToken)
    {
        var paymentIds = await OrderMapper.LoadPaymentIdsAsync(_db, [order.Id], cancellationToken);
        return OrderMapper.ToResponse(order, paymentIds.GetValueOrDefault(order.Id));
    }

    private async Task GiveBackPromotionUseAsync(Guid promotionId, string orderCode)
    {
        try
        {
            await _promotions.ReleaseAsync(promotionId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Order {OrderCode} was cancelled but the use of promotion {PromotionId} could not be given back.", orderCode, promotionId);
        }
    }

    private async Task NotifyStatusChangedAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken cancellationToken)
    {
        try
        {
            await _notifier.OrderStatusChangedAsync(
                new OrderNotification(order.Id, order.OrderCode, order.CustomerName, order.Phone, order.Email, order.TotalAmount),
                EnumWire.ToWire(from),
                EnumWire.ToWire(to),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Order {OrderCode} changed status but its notification failed.", order.OrderCode);
        }
    }
}

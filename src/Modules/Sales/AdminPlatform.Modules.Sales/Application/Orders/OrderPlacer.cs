using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Application.Settings;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdminPlatform.Modules.Sales.Application.Orders;

/// <param name="AlreadyPaid">The money was confirmed (a payment session) so the payment is created as paid.</param>
/// <param name="PaymentConfirmedBy">Who confirmed the money (display text for the payment's audit entry).</param>
internal sealed record PlaceOrderOptions(
    Guid? CustomerId,
    string? IdempotencyKey,
    bool AlreadyPaid,
    string PaymentConfirmedBy,
    string? Gateway,
    string? GatewayReference);

/// <summary>What placing produced. <see cref="IsReplay"/> is true when the idempotency key had already been
/// used and the earlier order is returned instead of a new one.</summary>
internal sealed record PlacedOrder(Order Order, Payment Payment, bool IsReplay);

/// <summary>Turns a priced request into a stored order + payment. One SaveChanges writes both (so they exist
/// together or not at all). The discount-code use is taken first, atomically, in the Catalog's own database
/// call; if the order then cannot be saved, the use is given back.</summary>
internal sealed class OrderPlacer
{
    /// <summary>The first line of every order's history: the customer placed it.</summary>
    internal const string CustomerActor = "Khách hàng";

    private readonly ISalesDbContext _db;
    private readonly IPromotionPricing _promotions;
    private readonly IOrderCodeGenerator _codes;
    private readonly IOrderNotifier _notifier;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OrderPlacer> _logger;

    public OrderPlacer(
        ISalesDbContext db,
        IPromotionPricing promotions,
        IOrderCodeGenerator codes,
        IOrderNotifier notifier,
        IDateTimeProvider clock,
        ILogger<OrderPlacer> logger)
    {
        _db = db;
        _promotions = promotions;
        _codes = codes;
        _notifier = notifier;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>The order already placed with this idempotency key, with its payment, or null.</summary>
    public async Task<(Order Order, Payment Payment)?> FindByIdempotencyKeyAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        var order = await _db.Orders.WithDetails().SingleOrDefaultAsync(o => o.IdempotencyKey == trimmed, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var payment = await _db.Payments.Include(p => p.Transactions).SingleAsync(p => p.OrderId == order.Id, cancellationToken);
        return (order, payment);
    }

    public async Task<PlacedOrder> PlaceAsync(
        CreateOrderRequest request, PricedOrder priced, PlaceOrderOptions options, CancellationToken cancellationToken)
    {
        var promotionId = priced.Promotion?.PromotionId;
        if (promotionId is { } id && !await _promotions.TryRedeemAsync(id, cancellationToken))
        {
            throw new ConflictException("Mã giảm giá đã hết lượt sử dụng.");
        }

        try
        {
            var now = _clock.UtcNow;
            var settings = await OrderSettingsStore.GetOrCreateAsync(_db, cancellationToken);
            var orderCode = await _codes.NextAsync(settings, cancellationToken);

            var order = Order.Create(
                new OrderDraft(
                    orderCode,
                    options.IdempotencyKey,
                    options.CustomerId,
                    request.CustomerName,
                    request.Phone,
                    request.Email,
                    priced.DeliveryAddress,
                    priced.PaymentMethod.Code,
                    priced.PaymentMethod.Name,
                    priced.DeliveryMethod.Code,
                    priced.DeliveryMethod.Name,
                    priced.DeliveryMethod.Type == DeliveryMethodType.Pickup,
                    priced.Items,
                    priced.OptionSelections,
                    priced.Discount,
                    priced.Promotion?.Code,
                    promotionId,
                    priced.ShippingDiscount,
                    priced.DeliveryFee,
                    request.WantsUtensils,
                    request.Note,
                    CustomerActor),
                now);

            var payment = Payment.CreateFor(order, options.Gateway, options.GatewayReference, now, options.PaymentConfirmedBy, options.AlreadyPaid);
            order.SyncPaymentStatus(payment.Status);

            _db.Orders.Add(order);
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);

            await NotifyPlacedAsync(order, cancellationToken);
            return new PlacedOrder(order, payment, false);
        }
        catch (Exception exception)
        {
            await ReleaseQuietlyAsync(promotionId);

            // Two identical submissions raced past the early lookup: the unique index let only one win, so the
            // loser answers with the winner's order instead of failing.
            if (exception is DbUpdateException && await FindByIdempotencyKeyAsync(options.IdempotencyKey, cancellationToken) is { } existing)
            {
                return new PlacedOrder(existing.Order, existing.Payment, true);
            }

            throw;
        }
    }

    private async Task NotifyPlacedAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            await _notifier.OrderPlacedAsync(
                new OrderNotification(order.Id, order.OrderCode, order.CustomerName, order.Phone, order.Email, order.TotalAmount),
                cancellationToken);
        }
        catch (Exception exception)
        {
            // A broken notification channel must never undo or fail an order that is already saved.
            _logger.LogWarning(exception, "Order {OrderCode} was placed but its notification failed.", order.OrderCode);
        }
    }

    private async Task ReleaseQuietlyAsync(Guid? promotionId)
    {
        if (promotionId is not { } id)
        {
            return;
        }

        try
        {
            await _promotions.ReleaseAsync(id, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not give back the use of promotion {PromotionId} after a failed order.", id);
        }
    }
}

using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.Orders;

internal static class OrderMapper
{
    /// <summary>Everything an <see cref="OrderResponse"/> needs, in one split query.</summary>
    public static IQueryable<Order> WithDetails(this IQueryable<Order> query) =>
        query.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .Include(o => o.OptionSelections)
            .Include(o => o.StatusHistory)
            .AsSplitQuery();

    public static async Task<Dictionary<Guid, Guid>> LoadPaymentIdsAsync(
        ISalesDbContext db, IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken) =>
        await db.Payments.AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId))
            .Select(p => new { p.OrderId, p.Id })
            .ToDictionaryAsync(p => p.OrderId, p => p.Id, cancellationToken);

    public static OrderResponse ToResponse(Order o, Guid? paymentId) => new(
        o.Id,
        o.OrderCode,
        o.CustomerId,
        o.CustomerName,
        o.Phone,
        o.Email,
        o.DeliveryAddressSnapshot,
        o.PaymentMethodCode,
        o.PaymentMethodLabel,
        o.DeliveryMethodCode,
        o.DeliveryMethodLabel,
        o.IsPickup,
        o.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.ProductName,
            i.ProductImageMediaId,
            i.UnitPrice,
            i.Quantity,
            i.LineTotal,
            i.ItemNote,
            i.Modifiers.Select(ToModifier).ToList())).ToList(),
        o.OptionSelections.Select(ToModifier).ToList(),
        o.StatusHistory.OrderBy(h => h.ChangedAtUtc).Select(h => new OrderStatusHistoryResponse(
            h.FromStatus is { } from ? EnumWire.ToWire(from) : null,
            EnumWire.ToWire(h.ToStatus),
            h.ChangedAtUtc,
            h.ChangedBy,
            h.Note)).ToList(),
        o.Subtotal,
        o.Discount,
        o.DiscountCode,
        o.PromotionId,
        o.ShippingDiscount,
        o.DeliveryFee,
        o.TotalAmount,
        EnumWire.ToWire(o.OrderStatus),
        EnumWire.ToWire(o.PaymentStatus),
        o.WantsUtensils,
        o.CustomerNote,
        o.NextStatuses.Select(s => EnumWire.ToWire(s)).ToList(),
        paymentId,
        o.CreatedAtUtc,
        o.UpdatedAtUtc ?? o.CreatedAtUtc,
        o.CompletedAtUtc,
        o.CancelledAtUtc);

    private static OrderModifierResponse ToModifier(OrderItemModifier m) =>
        new(m.GroupId, m.GroupName, m.OptionId, m.OptionLabel, m.PriceAdjustment);

    private static OrderModifierResponse ToModifier(OrderOptionSelection s) =>
        new(s.GroupId, s.GroupName, s.OptionId, s.OptionLabel, s.PriceAdjustment);
}

using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.Orders;

/// <summary>The result of pricing a request: the trusted methods, the snapshot lines and every amount.</summary>
internal sealed record PricedOrder(
    DeliveryMethod DeliveryMethod,
    PaymentMethod PaymentMethod,
    string DeliveryAddress,
    IReadOnlyList<OrderItem> Items,
    IReadOnlyList<OrderOptionSelection> OptionSelections,
    decimal Subtotal,
    decimal DeliveryFee,
    PromotionPricingResult? Promotion,
    decimal Discount,
    decimal ShippingDiscount,
    decimal TotalAmount);

/// <summary>The only place an order's money is decided. It re-reads every price from the catalog, every fee
/// from the delivery method and every discount from the promotion rules; the request's own numbers do not
/// exist. Used both to place an order and to quote a payment session.</summary>
internal sealed class OrderPricer
{
    private readonly ISalesDbContext _db;
    private readonly ICatalogPricingProvider _catalog;
    private readonly IPromotionPricing _promotions;

    public OrderPricer(ISalesDbContext db, ICatalogPricingProvider catalog, IPromotionPricing promotions)
    {
        _db = db;
        _catalog = catalog;
        _promotions = promotions;
    }

    public async Task<PricedOrder> PriceAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new BusinessRuleValidationException("Giỏ hàng đang trống.");
        }

        if (request.Items.Count > Order.MaxItemCount)
        {
            throw new BusinessRuleValidationException($"Một đơn hàng chỉ có tối đa {Order.MaxItemCount} dòng món.");
        }

        var deliveryCode = (request.DeliveryMethodCode ?? string.Empty).Trim().ToLowerInvariant();
        var deliveryMethod = await _db.DeliveryMethods.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Code == deliveryCode && m.IsActive, cancellationToken)
            ?? throw new BusinessRuleValidationException("Phương thức giao hàng không khả dụng.");

        var paymentCode = (request.PaymentMethodCode ?? string.Empty).Trim().ToLowerInvariant();
        var paymentMethod = await _db.PaymentMethods.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Code == paymentCode && m.IsActive, cancellationToken)
            ?? throw new BusinessRuleValidationException("Phương thức thanh toán không khả dụng.");

        string address;
        if (deliveryMethod.Type == DeliveryMethodType.Pickup)
        {
            address = deliveryMethod.PickupAddress ?? deliveryMethod.Name;
        }
        else if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new BusinessRuleValidationException("Vui lòng nhập địa chỉ giao hàng.");
        }
        else
        {
            address = request.DeliveryAddress.Trim();
        }

        var items = await BuildItemsAsync(request.Items, cancellationToken);
        var optionSelections = await BuildOptionSelectionsAsync(request.OrderOptions ?? [], cancellationToken);

        var subtotal = Money.Round(items.Sum(i => i.LineTotal) + optionSelections.Sum(o => o.PriceAdjustment));
        if (!paymentMethod.IsEligible(subtotal))
        {
            throw new BusinessRuleValidationException("Đơn hàng không đủ điều kiện áp dụng phương thức thanh toán này.");
        }

        var deliveryFee = Money.Round(deliveryMethod.ResolveFee(subtotal));

        PromotionPricingResult? promotion = null;
        decimal discount = 0;
        decimal shippingDiscount = 0;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            promotion = await _promotions.EvaluateAsync(
                request.DiscountCode.Trim(),
                subtotal,
                deliveryFee,
                items.Select(i => new PromotionLine(i.ProductId, i.LineTotal)).ToList(),
                cancellationToken);

            if (!promotion.IsValid || promotion.PromotionId is null)
            {
                throw new BusinessRuleValidationException(promotion.Message ?? "Mã giảm giá không hợp lệ.");
            }

            discount = Money.Round(Math.Min(promotion.Discount, subtotal));
            shippingDiscount = Money.Round(Math.Min(promotion.ShippingDiscount, deliveryFee));
        }

        var total = Money.Round(subtotal - discount + deliveryFee - shippingDiscount);
        return new PricedOrder(deliveryMethod, paymentMethod, address, items, optionSelections, subtotal, deliveryFee, promotion, discount, shippingDiscount, total);
    }

    private async Task<IReadOnlyList<OrderItem>> BuildItemsAsync(IReadOnlyList<OrderItemRequest> requests, CancellationToken cancellationToken)
    {
        var products = await _catalog.GetProductsAsync(requests.Select(r => r.ProductId).Distinct().ToList(), cancellationToken);
        var items = new List<OrderItem>(requests.Count);

        foreach (var request in requests)
        {
            if (request.Quantity is < 1 or > Order.MaxQuantity)
            {
                throw new BusinessRuleValidationException($"Số lượng mỗi món phải từ 1 đến {Order.MaxQuantity}.");
            }

            if (!products.TryGetValue(request.ProductId, out var product) || !product.IsActive)
            {
                throw new BusinessRuleValidationException("Có món trong giỏ hàng không còn được bán. Vui lòng xóa món đó và thử lại.");
            }

            var modifiers = new List<OrderItemModifier>();
            var picked = (request.Modifiers ?? []).Distinct().ToList();
            foreach (var selection in picked)
            {
                var group = product.ModifierGroups.FirstOrDefault(g => g.Id == selection.GroupId);
                var option = group?.Options.FirstOrDefault(o => o.Id == selection.OptionId);
                if (group is null || option is null)
                {
                    throw new BusinessRuleValidationException($"Tùy chọn của món '{product.Name}' không còn hợp lệ. Vui lòng thêm lại món vào giỏ.");
                }

                modifiers.Add(OrderItemModifier.Create(group.Id, group.Name, option.Id, option.Label, option.PriceAdjustment));
            }

            var tooMany = product.ModifierGroups.FirstOrDefault(g => !g.IsMultiple && modifiers.Count(m => m.GroupId == g.Id) > 1);
            if (tooMany is not null)
            {
                throw new BusinessRuleValidationException($"Nhóm '{tooMany.Name}' của món '{product.Name}' chỉ được chọn một lựa chọn.");
            }

            items.Add(OrderItem.Create(product.Id, product.Name, product.PrimaryMediaId, product.Price, request.Quantity, request.Note, modifiers));
        }

        return items;
    }

    private async Task<IReadOnlyList<OrderOptionSelection>> BuildOptionSelectionsAsync(
        IReadOnlyList<SelectedOptionRequest> requests, CancellationToken cancellationToken)
    {
        var picked = requests.Distinct().ToList();
        if (picked.Count == 0)
        {
            return [];
        }

        var groupIds = picked.Select(p => p.GroupId).Distinct().ToList();
        var groups = await _db.OrderOptionGroups.AsNoTracking()
            .Include(g => g.Values)
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        var selections = new List<OrderOptionSelection>(picked.Count);
        foreach (var selection in picked)
        {
            var group = groups.GetValueOrDefault(selection.GroupId);
            var value = group?.Values.FirstOrDefault(v => v.Id == selection.OptionId);
            if (group is null || value is null)
            {
                throw new BusinessRuleValidationException("Có tùy chọn chung của đơn hàng không còn hợp lệ. Vui lòng chọn lại.");
            }

            selections.Add(OrderOptionSelection.Create(group.Id, group.Name, value.Id, value.Label, value.PriceAdjustment));
        }

        var tooMany = groups.Values.FirstOrDefault(g =>
            g.SelectionType == OptionSelectionType.Single && selections.Count(s => s.GroupId == g.Id) > 1);
        if (tooMany is not null)
        {
            throw new BusinessRuleValidationException($"Nhóm '{tooMany.Name}' chỉ được chọn một lựa chọn.");
        }

        return selections;
    }
}

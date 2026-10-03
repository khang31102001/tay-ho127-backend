using System.Globalization;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

public sealed class PromotionValidationService : IPromotionValidationService
{
    /// <summary>Vietnamese grouping built by hand: the "vi-VN" culture does not exist in globalization-invariant
    /// runtimes (typical for slim container images), and a missing culture must not take the service down.</summary>
    private static readonly NumberFormatInfo VietnameseNumbers = new() { NumberGroupSeparator = ".", NumberDecimalSeparator = "," };

    private readonly ICatalogDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PromotionValidationService(ICatalogDbContext db, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ValidatePromotionResponse> ValidateAsync(ValidatePromotionRequest request, CancellationToken cancellationToken)
    {
        var code = Promotion.NormalizeCode(request.Code);
        if (code.Length == 0)
        {
            return Invalid("Vui lòng nhập mã giảm giá.");
        }

        var promotion = await _db.Promotions
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == code, cancellationToken);
        if (promotion is null)
        {
            return Invalid("Mã giảm giá không tồn tại hoặc đã bị xóa.");
        }

        var now = _dateTimeProvider.UtcNow;
        if (promotion.Status != PromotionStatus.Active)
        {
            return Invalid("Mã giảm giá hiện không khả dụng.");
        }

        if (promotion.IsExpired(now))
        {
            return Invalid("Mã giảm giá đã hết hạn.");
        }

        if (!promotion.HasStarted(now))
        {
            return Invalid("Mã giảm giá chưa đến ngày áp dụng.");
        }

        if (promotion.IsUsageExhausted)
        {
            return Invalid("Mã giảm giá đã hết lượt sử dụng.");
        }

        if (promotion.MinimumOrderAmount is { } minimum && minimum > 0 && request.Subtotal < minimum)
        {
            return Invalid($"Đơn hàng cần tối thiểu {FormatCurrency(minimum)} để áp dụng mã này.");
        }

        var summary = new PromotionSummary(promotion.Id, promotion.Code, promotion.Name, PromotionWireFormat.ToWire(promotion.Type));

        switch (promotion.Type)
        {
            case PromotionType.Percentage:
            case PromotionType.FixedAmount:
                var raw = promotion.Type == PromotionType.Percentage ? Percent(request.Subtotal, promotion.Value) : promotion.Value;
                return Valid(summary, Math.Min(Cap(raw, promotion.MaxDiscountAmount), request.Subtotal), 0);

            case PromotionType.FreeShipping:
                return Valid(summary, 0, Math.Min(Percent(request.ShippingFee, promotion.Value), request.ShippingFee));

            default:
                var eligibleSubtotal = await CalculateEligibleSubtotalAsync(promotion, request.Items ?? [], cancellationToken);
                if (eligibleSubtotal <= 0)
                {
                    return Invalid("Mã giảm giá không áp dụng cho sản phẩm nào trong giỏ hàng của bạn.");
                }

                var capped = Cap(Percent(eligibleSubtotal, promotion.Value), promotion.MaxDiscountAmount);
                return Valid(summary, Math.Min(capped, eligibleSubtotal), 0);
        }
    }

    /// <summary>Only the lines of the promotion's products — or of products under its categories, at any
    /// depth — count. The category of each product is looked up here, never taken from the client.</summary>
    private async Task<decimal> CalculateEligibleSubtotalAsync(
        Promotion promotion, IReadOnlyList<ValidatePromotionItem> items, CancellationToken cancellationToken)
    {
        var cartProductIds = items
            .Select(item => Guid.TryParse(item.ProductId, out var id) ? id : (Guid?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (cartProductIds.Count == 0)
        {
            return 0;
        }

        var scopedProductIds = promotion.Products.Select(link => link.ProductId).ToHashSet();
        var scopedCategoryIds = promotion.Categories.Select(link => link.CategoryId).ToHashSet();

        var categoryByProduct = await _db.Products.AsNoTracking()
            .Where(p => cartProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.CategoryId, cancellationToken);

        // The whole tree is small (3 levels), so one read gives every ancestry chain.
        var parentByCategory = scopedCategoryIds.Count == 0
            ? []
            : await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.ParentId, cancellationToken);

        bool IsEligible(Guid productId)
        {
            if (scopedProductIds.Contains(productId))
            {
                return true;
            }

            if (!categoryByProduct.TryGetValue(productId, out var categoryId))
            {
                return false;
            }

            Guid? current = categoryId;
            while (current is { } id)
            {
                if (scopedCategoryIds.Contains(id))
                {
                    return true;
                }

                current = parentByCategory.GetValueOrDefault(id);
            }

            return false;
        }

        return items.Sum(item => Guid.TryParse(item.ProductId, out var id) && IsEligible(id) ? item.LineTotal : 0);
    }

    private static decimal Percent(decimal amount, decimal percent) =>
        Math.Round(amount * percent / 100, MidpointRounding.AwayFromZero);

    private static decimal Cap(decimal amount, decimal? max) => max is { } cap && cap > 0 ? Math.Min(amount, cap) : amount;

    private static string FormatCurrency(decimal amount) => $"{amount.ToString("N0", VietnameseNumbers)}đ";

    private static ValidatePromotionResponse Valid(PromotionSummary summary, decimal discountAmount, decimal shippingDiscount) =>
        new(true, summary, discountAmount, shippingDiscount, null);

    private static ValidatePromotionResponse Invalid(string message) => new(false, null, 0, 0, message);
}

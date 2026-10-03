using AdminPlatform.Modules.Catalog.Application.Pricing;
using AdminPlatform.Modules.Catalog.Application.Promotions;
using AdminPlatform.Modules.Customer.Application.Customers;
using AdminPlatform.Modules.Sales.Application.Ports;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Sales' ICatalogPricingProvider port with the Catalog's ICatalogPricingQueryService. Lives in the
/// Host because only the Host may reference both modules (architecture assumption #6).</summary>
internal sealed class SalesCatalogPricingAdapter : ICatalogPricingProvider
{
    private readonly ICatalogPricingQueryService _catalog;

    public SalesCatalogPricingAdapter(ICatalogPricingQueryService catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyDictionary<Guid, PricedProduct>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var products = await _catalog.GetProductsAsync(productIds, cancellationToken);
        return products.ToDictionary(
            p => p.Id,
            p => new PricedProduct(
                p.Id,
                p.Name,
                p.Price,
                p.IsActive,
                p.PrimaryMediaId,
                p.ModifierGroups.Select(g => new PricedModifierGroup(
                    g.Id,
                    g.Name,
                    g.IsMultiple,
                    g.Options.Select(o => new PricedModifierOption(o.Id, o.Label, o.PriceAdjustment)).ToList())).ToList()));
    }
}

/// <summary>Implements Sales' IPromotionPricing port with the Catalog's existing code validation (the single place the
/// discount rules live) plus its atomic redemption counter.</summary>
internal sealed class SalesPromotionPricingAdapter : IPromotionPricing
{
    private readonly IPromotionValidationService _validation;
    private readonly IPromotionRedemptionService _redemption;

    public SalesPromotionPricingAdapter(IPromotionValidationService validation, IPromotionRedemptionService redemption)
    {
        _validation = validation;
        _redemption = redemption;
    }

    public async Task<PromotionPricingResult> EvaluateAsync(
        string code, decimal subtotal, decimal shippingFee, IReadOnlyList<PromotionLine> lines, CancellationToken cancellationToken)
    {
        var result = await _validation.ValidateAsync(
            new ValidatePromotionRequest(
                code,
                subtotal,
                shippingFee,
                lines.Select(l => new ValidatePromotionItem(l.ProductId.ToString(), l.LineTotal)).ToList()),
            cancellationToken);

        return new PromotionPricingResult(
            result.IsValid,
            result.Promotion?.Id,
            result.Promotion?.Code,
            result.DiscountAmount,
            result.ShippingDiscount,
            result.Message);
    }

    public Task<bool> TryRedeemAsync(Guid promotionId, CancellationToken cancellationToken) =>
        _redemption.TryRedeemAsync(promotionId, cancellationToken);

    public Task ReleaseAsync(Guid promotionId, CancellationToken cancellationToken) =>
        _redemption.ReleaseAsync(promotionId, cancellationToken);
}

/// <summary>Implements Sales' ICustomerDirectory port with the Customer module's lookup contract.</summary>
internal sealed class SalesCustomerDirectoryAdapter : ICustomerDirectory
{
    private readonly ICustomerLookupService _customers;

    public SalesCustomerDirectoryAdapter(ICustomerLookupService customers)
    {
        _customers = customers;
    }

    public Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken) =>
        _customers.ExistsActiveAsync(customerId, cancellationToken);
}

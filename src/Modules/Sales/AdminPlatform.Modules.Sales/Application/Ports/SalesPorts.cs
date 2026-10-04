namespace AdminPlatform.Modules.Sales.Application.Ports;

// Ports Sales needs from the rest of the system. Sales never references another module: each interface is
// implemented by a thin adapter in the Host (AdminPlatform.Api/CrossModuleAdapters) that calls the module
// that owns the data.

/// <summary>The authoritative price list: what a dish costs and which options it offers RIGHT NOW. An order
/// is priced from this, never from numbers the browser sends.</summary>
public interface ICatalogPricingProvider
{
    /// <summary>Only the ids that exist are returned (inactive ones included, flagged <see cref="PricedProduct.IsActive"/>).</summary>
    Task<IReadOnlyDictionary<Guid, PricedProduct>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}

public sealed record PricedProduct(
    Guid Id,
    string Name,
    decimal Price,
    bool IsActive,
    string? PrimaryMediaId,
    IReadOnlyList<PricedModifierGroup> ModifierGroups);

public sealed record PricedModifierGroup(Guid Id, string Name, bool IsMultiple, IReadOnlyList<PricedModifierOption> Options);

public sealed record PricedModifierOption(Guid Id, string Label, decimal PriceAdjustment);

/// <summary>Discount codes: re-evaluating a code against the real cart, and counting its use. Redemption is
/// atomic in the owning module, so two orders racing for the last use cannot both win.</summary>
public interface IPromotionPricing
{
    Task<PromotionPricingResult> EvaluateAsync(
        string code,
        decimal subtotal,
        decimal shippingFee,
        IReadOnlyList<PromotionLine> lines,
        CancellationToken cancellationToken);

    /// <summary>Takes one use of the promotion; false when its usage limit is already reached.</summary>
    Task<bool> TryRedeemAsync(Guid promotionId, CancellationToken cancellationToken);

    /// <summary>Gives one use back (an order that could not be saved, or was cancelled).</summary>
    Task ReleaseAsync(Guid promotionId, CancellationToken cancellationToken);
}

public sealed record PromotionLine(Guid ProductId, decimal LineTotal);

public sealed record PromotionPricingResult(bool IsValid, Guid? PromotionId, string? Code, decimal Discount, decimal ShippingDiscount, string? Message);

/// <summary>Whether a customer id from a token is a real, usable customer account. A guest has none.</summary>
public interface ICustomerDirectory
{
    Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken);
}

/// <summary>Where "your order was placed / its status changed" messages go. Today it only logs; plug an email
/// or SMS provider in by registering another implementation — Sales itself does not change.</summary>
public interface IOrderNotifier
{
    Task OrderPlacedAsync(OrderNotification notification, CancellationToken cancellationToken);

    Task OrderStatusChangedAsync(OrderNotification notification, string fromStatus, string toStatus, CancellationToken cancellationToken);
}

public sealed record OrderNotification(Guid OrderId, string OrderCode, string CustomerName, string Phone, string? Email, decimal TotalAmount);

/// <summary>Hands out the next order code. The running number comes from a counter incremented inside the
/// database, so concurrent orders never share a code.</summary>
public interface IOrderCodeGenerator
{
    Task<string> NextAsync(Domain.OrderSettings settings, CancellationToken cancellationToken);
}

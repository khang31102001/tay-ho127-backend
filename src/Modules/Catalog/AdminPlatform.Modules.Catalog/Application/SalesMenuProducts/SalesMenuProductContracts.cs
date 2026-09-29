namespace AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;

/// <summary>PriceOverride null = the product's own price applies on this menu.</summary>
public sealed record CreateSalesMenuProductRequest(Guid SalesMenuId, Guid ProductId, decimal? PriceOverride, int SortOrder, bool IsAvailable);

public sealed record UpdateSalesMenuProductRequest(Guid SalesMenuId, Guid ProductId, decimal? PriceOverride, int SortOrder, bool IsAvailable);

public sealed record SalesMenuProductResponse(
    Guid Id,
    Guid SalesMenuId,
    Guid ProductId,
    decimal? PriceOverride,
    int SortOrder,
    bool IsAvailable,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

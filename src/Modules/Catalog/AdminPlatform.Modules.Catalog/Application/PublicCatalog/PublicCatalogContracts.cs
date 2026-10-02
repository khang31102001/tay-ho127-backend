namespace AdminPlatform.Modules.Catalog.Application.PublicCatalog;

/// <summary>Read model of everything the public website sells, returned in one anonymous call so a page
/// renders from one consistent snapshot. Only active categories, products and menus are included, and only
/// available placements of active products on those menus. Audit fields and admin-only state stay out.</summary>
public sealed record PublicCatalogResponse(
    IReadOnlyList<PublicCategoryResponse> Categories,
    IReadOnlyList<PublicProductResponse> Products,
    IReadOnlyList<PublicSalesMenuResponse> SalesMenus,
    IReadOnlyList<PublicSalesMenuProductResponse> SalesMenuProducts,
    IReadOnlyList<PublicModifierGroupResponse> ModifierGroups);

public sealed record PublicCategoryResponse(Guid Id, string Name, Guid? ParentId, int SortOrder);

public sealed record PublicProductResponse(
    Guid Id,
    string Slug,
    string Name,
    Guid CategoryId,
    decimal Price,
    decimal? OldPrice,
    string? Description,
    string? Badge,
    decimal? Rating,
    int? RatingCount,
    IReadOnlyList<string> MediaIds,
    IReadOnlyList<Guid> ModifierGroupIds);

public sealed record PublicSalesMenuResponse(Guid Id, string Code, string Name);

public sealed record PublicSalesMenuProductResponse(Guid Id, Guid SalesMenuId, Guid ProductId, decimal? PriceOverride, int SortOrder);

public sealed record PublicModifierOptionResponse(Guid Id, string Label, decimal PriceAdjustment, bool IsDefault);

public sealed record PublicModifierGroupResponse(
    Guid Id,
    string Name,
    string SelectionType,
    bool IsRequired,
    IReadOnlyList<PublicModifierOptionResponse> Options);

namespace AdminPlatform.Modules.Catalog.Application.Products;

/// <summary>Description is the short plain-text summary; Content is the rich-text detail (HTML, sanitized on
/// save, max 1000 words). The list endpoint returns Content as null — open the product to get it. On update,
/// Content null keeps the current detail and an empty string clears it. Slug is optional: when blank it is generated from Name (and made unique). MediaIds and
/// ModifierGroupIds are ordered lists that fully replace the product's current links.</summary>
public sealed record CreateProductRequest(
    string Name,
    string? Slug,
    Guid CategoryId,
    decimal Price,
    decimal? OldPrice,
    string? Description,
    string? Content,
    bool IsActive,
    string? Badge,
    decimal? Rating,
    int? RatingCount,
    IReadOnlyList<string>? MediaIds,
    IReadOnlyList<Guid>? ModifierGroupIds);

public sealed record UpdateProductRequest(
    string Name,
    string? Slug,
    Guid CategoryId,
    decimal Price,
    decimal? OldPrice,
    string? Description,
    string? Content,
    bool IsActive,
    string? Badge,
    decimal? Rating,
    int? RatingCount,
    IReadOnlyList<string>? MediaIds,
    IReadOnlyList<Guid>? ModifierGroupIds);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid CategoryId,
    decimal Price,
    decimal? OldPrice,
    string? Description,
    string? Content,
    bool IsActive,
    string? Badge,
    decimal? Rating,
    int? RatingCount,
    IReadOnlyList<string> MediaIds,
    IReadOnlyList<Guid> ModifierGroupIds,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

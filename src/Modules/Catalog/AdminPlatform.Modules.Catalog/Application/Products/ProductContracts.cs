namespace AdminPlatform.Modules.Catalog.Application.Products;

/// <summary>Slug is optional: when blank it is generated from Name (and made unique). MediaIds and
/// ModifierGroupIds are ordered lists that fully replace the product's current links.</summary>
public sealed record CreateProductRequest(
    string Name,
    string? Slug,
    Guid CategoryId,
    decimal Price,
    decimal? OldPrice,
    string? Description,
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
    bool IsActive,
    string? Badge,
    decimal? Rating,
    int? RatingCount,
    IReadOnlyList<string> MediaIds,
    IReadOnlyList<Guid> ModifierGroupIds,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

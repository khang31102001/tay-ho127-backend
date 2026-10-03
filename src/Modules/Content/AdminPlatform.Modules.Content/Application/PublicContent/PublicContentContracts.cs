namespace AdminPlatform.Modules.Content.Application.PublicContent;

/// <summary>A published article as the website lists it — no HTML body, but the reading time is computed
/// from it so the list does not need the body.</summary>
public sealed record PublicArticleSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid> TagIds,
    string AuthorName,
    DateTime PublishedAt,
    DateTime UpdatedAt,
    int ReadingTimeMinutes);

/// <summary>A published article with its (sanitized) HTML body.</summary>
public sealed record PublicArticleResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Content,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid> TagIds,
    string AuthorName,
    DateTime PublishedAt,
    DateTime UpdatedAt,
    int ReadingTimeMinutes);

public sealed record PublicArticleCategoryResponse(Guid Id, string Name, string Slug, Guid? ParentId, int SortOrder);

public sealed record PublicArticleTagResponse(Guid Id, string Name, string Slug);

/// <summary>Active categories (by sort order) and every tag — the website's filters.</summary>
public sealed record PublicTaxonomyResponse(
    IReadOnlyList<PublicArticleCategoryResponse> Categories,
    IReadOnlyList<PublicArticleTagResponse> Tags);

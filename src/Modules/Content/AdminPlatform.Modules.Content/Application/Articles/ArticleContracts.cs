namespace AdminPlatform.Modules.Content.Application.Articles;

/// <summary>Slug is optional: when blank it is generated from Title (and made unique). Content is HTML and is
/// sanitized on save. Status: "draft" | "published" | "archived" (case-insensitive); PublishedAt is set
/// automatically the first time the article is published. TagIds fully replaces the article's tags.</summary>
public sealed record CreateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid>? TagIds,
    string AuthorName,
    string Status);

/// <summary>A blank slug keeps the current one: editing a title must not silently change the public URL.</summary>
public sealed record UpdateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid>? TagIds,
    string AuthorName,
    string Status);

/// <summary>Full article (single read and write responses).</summary>
public sealed record ArticleResponse(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid> TagIds,
    string AuthorName,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>Row of the admin list — without the (potentially large) HTML Content.</summary>
public sealed record ArticleListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string? FeaturedMediaId,
    Guid? CategoryId,
    IReadOnlyList<Guid> TagIds,
    string AuthorName,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

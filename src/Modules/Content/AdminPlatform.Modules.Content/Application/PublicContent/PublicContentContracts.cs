using AdminPlatform.Modules.Content.Application.PageSections;

namespace AdminPlatform.Modules.Content.Application.PublicContent;

/// <summary>A published article as the website lists it — no HTML body, but the reading time is computed
/// from it so the list does not need the body. IsFeatured/FeaturedOrder drive the "Tin nổi bật" block.</summary>
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
    int ReadingTimeMinutes,
    bool IsFeatured,
    int? FeaturedOrder);

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
    int ReadingTimeMinutes,
    bool IsFeatured,
    int? FeaturedOrder);

public sealed record PublicArticleCategoryResponse(Guid Id, string Name, string Slug, Guid? ParentId, int SortOrder);

public sealed record PublicArticleTagResponse(Guid Id, string Name, string Slug);

/// <summary>Active categories (by sort order) and every tag — the website's filters.</summary>
public sealed record PublicTaxonomyResponse(
    IReadOnlyList<PublicArticleCategoryResponse> Categories,
    IReadOnlyList<PublicArticleTagResponse> Tags);

/// <summary>A live banner as the website shows it (no admin-only fields). Placement is the wire name, e.g. "HOME_HERO".</summary>
public sealed record PublicBannerResponse(
    Guid Id,
    string Name,
    string? DesktopMediaId,
    string? MobileMediaId,
    string AltText,
    string? Heading,
    string? Subheading,
    string? CtaLabel,
    string? CtaUrl,
    string Placement,
    int DisplayOrder);

/// <summary>A published page's identity — enough for the navigation to turn a page reference into its URL (Slug is the page's URL path).
/// PageKey is the stable identifier the website looks the page up by (null for a page it does not place itself).</summary>
public sealed record PublicPageResponse(Guid Id, string Name, string Slug, string? PageKey = null);

/// <summary>A visible section of a published page (no admin-only fields). SectionKind is the wire name, e.g. "story";
/// SectionKey is the website slot it fills (null for a free section). Body is plain text.</summary>
public sealed record PublicPageSectionResponse(
    Guid Id,
    string? SectionKey,
    string SectionKind,
    string? Eyebrow,
    string? Heading,
    string? Subheading,
    string? Body,
    string? MediaId,
    string? CtaLabel,
    string? CtaUrl,
    int DisplayOrder,
    PageSectionSettingsResponse? Settings);

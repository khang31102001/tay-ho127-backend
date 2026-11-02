namespace AdminPlatform.Modules.Content.Application.PageSections;

/// <summary>SectionKind: "hero" | "introduction" | "promotion" | "highlight" | "testimonial" | "cta" | "story" | "award" |
/// "stats" | "timeline" | "faq" | "core" (case-insensitive).
/// Body is plain text. CtaUrl must be a site path or an http(s) URL. Settings is optional: omitted or null clears it.
/// SectionKey is the website slot the section fills ("hero", "signature-dish"), unique per page; omitted or null = a
/// free section.</summary>
public sealed record CreatePageSectionRequest(
    string SectionKind,
    string? Eyebrow,
    string? Heading,
    string? Subheading,
    string? Body,
    string? MediaId,
    string? CtaLabel,
    string? CtaUrl,
    int DisplayOrder,
    bool IsVisible,
    PageSectionSettingsRequest? Settings = null,
    string? SectionKey = null);

public sealed record UpdatePageSectionRequest(
    string SectionKind,
    string? Eyebrow,
    string? Heading,
    string? Subheading,
    string? Body,
    string? MediaId,
    string? CtaLabel,
    string? CtaUrl,
    int DisplayOrder,
    bool IsVisible,
    PageSectionSettingsRequest? Settings = null,
    string? SectionKey = null);

public sealed record PageSectionResponse(
    Guid Id,
    Guid PageId,
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
    bool IsVisible,
    PageSectionSettingsResponse? Settings,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>Kind-specific extras. GalleryMediaIds: secondary images in display order (at most 6). Items: rows of a
/// stats/timeline/faq section in display order (at most 20) — stats needs label + title, timeline/faq title + text.</summary>
public sealed record PageSectionSettingsRequest(
    IReadOnlyList<string?>? GalleryMediaIds,
    IReadOnlyList<PageSectionItemRequest?>? Items = null);

public sealed record PageSectionItemRequest(string? Label, string? Title, string? Text, string? MediaId, string? Url);

public sealed record PageSectionSettingsResponse(IReadOnlyList<string> GalleryMediaIds, IReadOnlyList<PageSectionItemResponse> Items);

public sealed record PageSectionItemResponse(string? Label, string? Title, string? Text, string? MediaId, string? Url);

/// <summary>The new order of a page's sections: every section id of the page exactly once, first to last.</summary>
public sealed record ReorderPageSectionsRequest(IReadOnlyList<Guid> SectionIds);

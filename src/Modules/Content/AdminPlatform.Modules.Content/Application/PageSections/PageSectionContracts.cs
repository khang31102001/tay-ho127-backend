namespace AdminPlatform.Modules.Content.Application.PageSections;

/// <summary>SectionKind: "hero" | "introduction" | "promotion" | "highlight" | "testimonial" | "cta" | "story" (case-insensitive).
/// Body is plain text. CtaUrl must be a site path or an http(s) URL. Settings is optional: omitted or null clears it.</summary>
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
    PageSectionSettingsRequest? Settings = null);

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
    PageSectionSettingsRequest? Settings = null);

public sealed record PageSectionResponse(
    Guid Id,
    Guid PageId,
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

/// <summary>Kind-specific extras. GalleryMediaIds: secondary images in display order (at most 6).</summary>
public sealed record PageSectionSettingsRequest(IReadOnlyList<string?>? GalleryMediaIds);

public sealed record PageSectionSettingsResponse(IReadOnlyList<string> GalleryMediaIds);

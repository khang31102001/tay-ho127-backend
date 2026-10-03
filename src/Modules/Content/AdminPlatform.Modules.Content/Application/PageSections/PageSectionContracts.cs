namespace AdminPlatform.Modules.Content.Application.PageSections;

/// <summary>SectionKind: "hero" | "introduction" | "promotion" | "highlight" | "testimonial" | "cta" (case-insensitive).
/// Body is plain text. CtaUrl must be a site path or an http(s) URL.</summary>
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
    bool IsVisible);

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
    bool IsVisible);

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
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

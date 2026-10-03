namespace AdminPlatform.Modules.Seo.Application.Metadata;

/// <summary>EntityType: "product" | "category" | "article" | "page" | "homepage" (case-insensitive). EntityId is
/// required except for "homepage" (ignored there). Every text field is optional; blank = not configured.
/// CanonicalUrl must be a site path or an http(s) URL.</summary>
public sealed record UpsertSeoMetadataRequest(
    string EntityType,
    string? EntityId,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool RobotsIndex,
    bool RobotsFollow,
    string? OgTitle,
    string? OgDescription,
    string? OgImageMediaId,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImageMediaId);

public sealed record SeoMetadataResponse(
    Guid Id,
    string EntityType,
    string? EntityId,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool RobotsIndex,
    bool RobotsFollow,
    string? OgTitle,
    string? OgDescription,
    string? OgImageMediaId,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImageMediaId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>The identity of an entity the website must not index — used to leave it out of the sitemap.</summary>
public sealed record NoIndexEntityResponse(string EntityType, string? EntityId);

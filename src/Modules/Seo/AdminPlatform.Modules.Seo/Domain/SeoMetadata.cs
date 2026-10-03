using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>The per-entity SEO override: every text field is optional — a missing value means "not configured",
/// and the website falls back to the entity's own data, then to the site-wide <see cref="SeoSettings"/>.
/// One row per (EntityType, EntityId); the homepage has no EntityId.</summary>
public sealed class SeoMetadata : AuditableEntity
{
    public const int MaxEntityIdLength = 100;
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 500;
    public const int MaxUrlLength = 500;
    public const int MaxMediaIdLength = 64;

    public SeoEntityType EntityType { get; private set; }

    /// <summary>The overridden entity's id as the website knows it (product/category/article/page id). An opaque
    /// string with no FK — modules never share tables, and an override must survive its entity being renamed.
    /// Null only for <see cref="SeoEntityType.Homepage"/>.</summary>
    public string? EntityId { get; private set; }

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    /// <summary>A site path ("/thuc-don/x") or an absolute http(s) URL; null = generated from the page path.</summary>
    public string? CanonicalUrl { get; private set; }

    public bool RobotsIndex { get; private set; } = true;
    public bool RobotsFollow { get; private set; } = true;
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }

    /// <summary>Opaque media references with no FK (a Media module id, or a legacy key the website resolves itself).</summary>
    public string? OgImageMediaId { get; private set; }

    public string? TwitterTitle { get; private set; }
    public string? TwitterDescription { get; private set; }
    public string? TwitterImageMediaId { get; private set; }

    private SeoMetadata()
    {
        // EF Core
    }

    public static SeoMetadata Create(SeoEntityType entityType, string? entityId, SeoMetadataDetails details)
    {
        var metadata = new SeoMetadata
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = NormalizeEntityId(entityType, entityId),
        };
        metadata.Update(details);
        return metadata;
    }

    public void Update(SeoMetadataDetails details)
    {
        if (!string.IsNullOrWhiteSpace(details.CanonicalUrl) && !IsValidCanonical(details.CanonicalUrl))
        {
            throw new BusinessRuleValidationException("The canonical URL must be a site path (/...) or an http(s) URL.");
        }

        MetaTitle = Clean(details.MetaTitle);
        MetaDescription = Clean(details.MetaDescription);
        CanonicalUrl = Clean(details.CanonicalUrl);
        RobotsIndex = details.RobotsIndex;
        RobotsFollow = details.RobotsFollow;
        OgTitle = Clean(details.OgTitle);
        OgDescription = Clean(details.OgDescription);
        OgImageMediaId = Clean(details.OgImageMediaId);
        TwitterTitle = Clean(details.TwitterTitle);
        TwitterDescription = Clean(details.TwitterDescription);
        TwitterImageMediaId = Clean(details.TwitterImageMediaId);
    }

    /// <summary>Every type except the homepage needs an id; the homepage never has one (a supplied id is dropped,
    /// so the (type, id) key stays unique for the singleton).</summary>
    public static string? NormalizeEntityId(SeoEntityType entityType, string? entityId)
    {
        if (entityType == SeoEntityType.Homepage)
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(entityId, nameof(entityId)).Trim();
    }

    /// <summary>Same rule as any admin-entered link: a site-relative path (not "//host") or an absolute
    /// http(s) URL — never "javascript:" or "data:".</summary>
    public static bool IsValidCanonical(string url)
    {
        var value = url.Trim();
        if (value.StartsWith('/'))
        {
            return !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal);
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>The editable fields of an SEO override, grouped so Create/Update stay readable.</summary>
public sealed record SeoMetadataDetails(
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

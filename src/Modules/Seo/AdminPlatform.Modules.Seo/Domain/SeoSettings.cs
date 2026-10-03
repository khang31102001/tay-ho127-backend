using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>The site-wide SEO fallbacks (title template, default description/share image, default robots
/// directives, robots.txt disallow list). A singleton: exactly one row, with the well-known <see cref="SingletonId"/>.
/// Identity data (site name, logo, address, social links) is NOT stored here — it belongs to the brand settings.</summary>
public sealed class SeoSettings : AuditableEntity
{
    public static readonly Guid SingletonId = new("5e0c0000-0000-4000-8000-000000000001");

    public const string TitlePlaceholder = "%s";
    public const int MaxTitleTemplateLength = 200;
    public const int MaxDescriptionLength = 500;
    public const int MaxMediaIdLength = 64;
    public const int MaxTwitterHandleLength = 50;
    public const int MaxDisallowPaths = 100;
    public const int MaxDisallowPathLength = 200;

    public string DefaultTitleTemplate { get; private set; } = string.Empty;
    public string DefaultDescription { get; private set; } = string.Empty;

    /// <summary>Opaque media reference with no FK (a Media module id, or a legacy key the website resolves itself).</summary>
    public string? DefaultOgImageMediaId { get; private set; }

    public string? TwitterSite { get; private set; }
    public string? TwitterCreator { get; private set; }
    public bool DefaultRobotsIndex { get; private set; } = true;
    public bool DefaultRobotsFollow { get; private set; } = true;

    /// <summary>robots.txt "Disallow" paths (each starts with "/"), site-wide — distinct from the per-page
    /// index/follow defaults above.</summary>
    public List<string> RobotsDisallowPaths { get; private set; } = [];

    private SeoSettings()
    {
        // EF Core
    }

    public static SeoSettings Create(SeoSettingsDetails details)
    {
        var settings = new SeoSettings { Id = SingletonId };
        settings.Update(details);
        return settings;
    }

    public void Update(SeoSettingsDetails details)
    {
        var titleTemplate = Guard.NotNullOrWhiteSpace(details.DefaultTitleTemplate, nameof(details.DefaultTitleTemplate)).Trim();
        if (!titleTemplate.Contains(TitlePlaceholder, StringComparison.Ordinal))
        {
            throw new BusinessRuleValidationException($"The title template must contain the placeholder {TitlePlaceholder} for the page title.");
        }

        DefaultTitleTemplate = titleTemplate;
        DefaultDescription = Guard.NotNullOrWhiteSpace(details.DefaultDescription, nameof(details.DefaultDescription)).Trim();
        DefaultOgImageMediaId = Clean(details.DefaultOgImageMediaId);
        TwitterSite = Clean(details.TwitterSite);
        TwitterCreator = Clean(details.TwitterCreator);
        DefaultRobotsIndex = details.DefaultRobotsIndex;
        DefaultRobotsFollow = details.DefaultRobotsFollow;
        RobotsDisallowPaths = NormalizePaths(details.RobotsDisallowPaths);
    }

    /// <summary>A robots path is a site path: it starts with a single "/" and has no whitespace — a "//host"
    /// or "http://..." value would not be a robots.txt path at all.</summary>
    public static bool IsValidPath(string path) =>
        path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) && !path.Any(char.IsWhiteSpace);

    /// <summary>Trims, drops blanks and duplicates (keeping the first occurrence's order) and rejects anything
    /// that is not a valid path.</summary>
    private static List<string> NormalizePaths(IEnumerable<string> paths)
    {
        var result = new List<string>();
        foreach (var raw in paths)
        {
            var path = raw?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (!IsValidPath(path))
            {
                throw new BusinessRuleValidationException($"'{path}' is not a valid robots path — it must start with a single '/' and contain no spaces.");
            }

            if (!result.Contains(path, StringComparer.Ordinal))
            {
                result.Add(path);
            }
        }

        return result;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>The editable fields of the SEO settings, grouped so Create/Update stay readable.</summary>
public sealed record SeoSettingsDetails(
    string DefaultTitleTemplate,
    string DefaultDescription,
    string? DefaultOgImageMediaId,
    string? TwitterSite,
    string? TwitterCreator,
    bool DefaultRobotsIndex,
    bool DefaultRobotsFollow,
    IReadOnlyList<string> RobotsDisallowPaths);

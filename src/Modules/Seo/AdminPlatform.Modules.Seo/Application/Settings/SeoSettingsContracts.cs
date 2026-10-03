namespace AdminPlatform.Modules.Seo.Application.Settings;

/// <summary>RobotsDisallowPaths: site paths starting with "/" (e.g. "/admin"); blanks and duplicates are dropped.
/// DefaultTitleTemplate must contain "%s" (replaced by the page title).</summary>
public sealed record UpdateSeoSettingsRequest(
    string DefaultTitleTemplate,
    string DefaultDescription,
    string? DefaultOgImageMediaId,
    string? TwitterSite,
    string? TwitterCreator,
    bool DefaultRobotsIndex,
    bool DefaultRobotsFollow,
    IReadOnlyList<string> RobotsDisallowPaths);

public sealed record SeoSettingsResponse(
    string DefaultTitleTemplate,
    string DefaultDescription,
    string? DefaultOgImageMediaId,
    string? TwitterSite,
    string? TwitterCreator,
    bool DefaultRobotsIndex,
    bool DefaultRobotsFollow,
    IReadOnlyList<string> RobotsDisallowPaths,
    DateTime UpdatedAt);

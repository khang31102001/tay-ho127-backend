namespace AdminPlatform.Modules.Seo.Application.Redirects;

/// <summary>SourcePath: an old site path ("/thuc-don/mon-cu"); a missing leading "/" and a trailing "/" are fixed up.
/// DestinationUrl: a site path or an http(s) URL. RedirectType: 301 (permanent) or 302 (temporary).
/// Matching is exact on the path — no wildcards.</summary>
public sealed record CreateRedirectRequest(string SourcePath, string DestinationUrl, int RedirectType, bool IsActive);

public sealed record UpdateRedirectRequest(string SourcePath, string DestinationUrl, int RedirectType, bool IsActive);

public sealed record RedirectResponse(
    Guid Id,
    string SourcePath,
    string DestinationUrl,
    int RedirectType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>A live redirect as the website's middleware applies it (no admin-only fields).</summary>
public sealed record PublicRedirectResponse(string SourcePath, string DestinationUrl, int RedirectType);

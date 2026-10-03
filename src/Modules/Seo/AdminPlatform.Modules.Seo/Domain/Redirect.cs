using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>HTTP status of a redirect — the numeric value IS the status code, and is also the wire value.</summary>
public enum RedirectType
{
    /// <summary>301 — the old URL moved for good (search engines transfer its ranking).</summary>
    Permanent = 301,

    /// <summary>302 — a temporary detour.</summary>
    Temporary = 302,
}

/// <summary>Sends visitors (and search engines) from an old site path to a new URL, so renaming a slug does not
/// produce a 404. Deliberately NOT linked to any entity: the redirect must keep working even after the
/// product/article it once pointed at is deleted. Matching is exact on the path (no wildcards or regex).</summary>
public sealed class Redirect : AuditableEntity
{
    public const int MaxPathLength = 500;

    /// <summary>Paths the website's middleware never redirects (admin portal, API, Next.js internals) — a
    /// redirect on them could lock admins out, so they are rejected up front.</summary>
    private static readonly string[] ReservedPrefixes = ["/admin", "/api", "/_next"];

    /// <summary>Normalized site path: starts with "/", no trailing "/", no query/fragment/whitespace. Unique.</summary>
    public string SourcePath { get; private set; } = string.Empty;

    /// <summary>A site path ("/thuc-don/x", may carry a query) or an absolute http(s) URL.</summary>
    public string DestinationUrl { get; private set; } = string.Empty;

    public RedirectType RedirectType { get; private set; } = RedirectType.Permanent;
    public bool IsActive { get; private set; } = true;

    private Redirect()
    {
        // EF Core
    }

    public static Redirect Create(RedirectDetails details)
    {
        var redirect = new Redirect { Id = Guid.NewGuid() };
        redirect.Update(details);
        return redirect;
    }

    public void Update(RedirectDetails details)
    {
        var source = NormalizeSourcePath(details.SourcePath);
        var destination = Guard.NotNullOrWhiteSpace(details.DestinationUrl, nameof(details.DestinationUrl)).Trim();

        if (!IsValidDestination(destination))
        {
            throw new BusinessRuleValidationException("The destination must be a site path (/...) or an http(s) URL.");
        }

        if (TryGetDestinationPath(destination, out var destinationPath) && destinationPath == source)
        {
            throw new BusinessRuleValidationException("The source path and the destination must differ (it would redirect to itself).");
        }

        if (!Enum.IsDefined(details.RedirectType))
        {
            throw new BusinessRuleValidationException("The redirect type must be 301 or 302.");
        }

        SourcePath = source;
        DestinationUrl = destination;
        RedirectType = details.RedirectType;
        IsActive = details.IsActive;
    }

    /// <summary>Trims, adds a leading "/" and drops a trailing "/" so "thuc-don/x/" and "/thuc-don/x" are the same
    /// source. Rejects the site root, a query/fragment, whitespace, a protocol-relative "//host" and reserved areas.</summary>
    public static string NormalizeSourcePath(string path)
    {
        var value = Guard.NotNullOrWhiteSpace(path, nameof(path)).Trim();
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        if (value.Length > 1)
        {
            value = value.TrimEnd('/');
        }

        if (value.Length == 0 || value == "/")
        {
            throw new BusinessRuleValidationException("The source path cannot be the site root.");
        }

        if (value.StartsWith("//", StringComparison.Ordinal) || value.Any(c => char.IsWhiteSpace(c) || c is '?' or '#' or '\\'))
        {
            throw new BusinessRuleValidationException("The source path must be a plain site path: no spaces, query string, fragment or '//'.");
        }

        if (ReservedPrefixes.Any(prefix => IsUnder(value, prefix)))
        {
            throw new BusinessRuleValidationException("The source path cannot be under /admin, /api or /_next.");
        }

        return value;
    }

    /// <summary>Same rule as any admin-entered link: a site-relative path (not "//host") or an absolute http(s) URL —
    /// never "javascript:" or "data:".</summary>
    public static bool IsValidDestination(string url)
    {
        if (url.StartsWith('/'))
        {
            return !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal);
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }

    /// <summary>The destination's own path (query and fragment dropped, trailing "/" removed) when it points inside
    /// the site; false for an absolute URL. Used to detect redirects that loop back.</summary>
    public static bool TryGetDestinationPath(string destinationUrl, out string path)
    {
        path = string.Empty;
        if (!destinationUrl.StartsWith('/'))
        {
            return false;
        }

        var end = destinationUrl.IndexOfAny(['?', '#']);
        var value = end < 0 ? destinationUrl : destinationUrl[..end];
        path = value.Length > 1 ? value.TrimEnd('/') : value;
        return true;
    }

    private static bool IsUnder(string path, string prefix) =>
        path.Equals(prefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>The editable fields of a redirect, grouped so Create/Update stay readable.</summary>
public sealed record RedirectDetails(string SourcePath, string DestinationUrl, RedirectType RedirectType, bool IsActive);

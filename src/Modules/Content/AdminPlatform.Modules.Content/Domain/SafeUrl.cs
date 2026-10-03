namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Rule for any admin-entered URL that the website renders as a link (banner and section calls to
/// action): a site-relative path ("/thuc-don") or an absolute http(s) URL — never "javascript:", "data:" or a
/// protocol-relative ("//host") URL.</summary>
public static class SafeUrl
{
    public static bool IsSafe(string url)
    {
        var value = url.Trim();
        if (value.StartsWith('/'))
        {
            return !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal);
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }
}

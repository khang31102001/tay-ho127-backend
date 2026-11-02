using System.Text.RegularExpressions;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Stable code-facing identifiers the website looks content up by (a page's "home", a section's "hero"):
/// lowercase words joined by hyphens, at most <see cref="MaxLength"/> characters. Unlike generated ids they are the
/// same in every database, so seeds, SQL scripts and the website can name them.</summary>
public static partial class ContentKey
{
    public const int MaxLength = 64;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex KeyRegex();

    /// <summary>True for null (no key) or a well-formed key.</summary>
    public static bool IsValid(string? key) => key is null || (key.Length <= MaxLength && KeyRegex().IsMatch(key));

    /// <summary>Trimmed key, null when blank.</summary>
    public static string? Clean(string? key) => string.IsNullOrWhiteSpace(key) ? null : key.Trim();
}

using System.Net;
using System.Text.RegularExpressions;

namespace AdminPlatform.SharedKernel;

/// <summary>Word counting for rich-text HTML, matching what the admin editor shows in its status bar
/// (visible text only: tags are dropped and entities decoded before splitting on whitespace).</summary>
public static partial class HtmlText
{
    public static int CountWords(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return 0;
        }

        var text = WebUtility.HtmlDecode(TagPattern().Replace(html, " "));
        return WhitespacePattern().Split(text).Count(word => word.Length > 0);
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}

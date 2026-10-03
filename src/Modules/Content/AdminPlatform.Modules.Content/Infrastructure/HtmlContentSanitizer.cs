using AdminPlatform.Modules.Content.Application;
using Ganss.Xss;

namespace AdminPlatform.Modules.Content.Infrastructure;

/// <summary>Whitelist sanitizer (HtmlSanitizer) for the HTML produced by the admin's rich-text editor: the
/// default whitelist keeps the formatting the editor can produce (headings, lists, links, images,
/// quotes, tables) and drops scripts, event-handler attributes, javascript: URLs, iframes and forms.
/// Links are limited to http(s)/mailto/tel.</summary>
public sealed class HtmlContentSanitizer : IHtmlContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "tel" })
        {
            _sanitizer.AllowedSchemes.Add(scheme);
        }
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}

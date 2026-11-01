using AdminPlatform.Modules.Catalog.Application.Products;
using Ganss.Xss;

namespace AdminPlatform.Modules.Catalog.Infrastructure;

/// <summary>Same whitelist as the Content module's article sanitizer (modules do not reference each other):
/// keeps the editor's formatting (headings, lists, links, images, tables) and drops scripts, event handlers,
/// javascript:/data: URLs, iframes and forms. Links are limited to http(s)/mailto/tel.</summary>
public sealed class ProductContentSanitizer : IProductContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public ProductContentSanitizer()
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

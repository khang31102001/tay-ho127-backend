namespace AdminPlatform.Modules.Content.Application;

/// <summary>Strips anything executable (scripts, event handlers, javascript: URLs, iframes...) from
/// rich-text HTML before it is stored, because the public website renders it as raw HTML.
/// Implemented in Infrastructure.</summary>
public interface IHtmlContentSanitizer
{
    string Sanitize(string html);
}

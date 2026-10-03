using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>One content block of a <see cref="Page"/>. A section belongs to exactly one page and is
/// deleted with it. <see cref="Body"/> is PLAIN TEXT (the admin edits it in a text area), not HTML.</summary>
public sealed class PageSection : AuditableEntity
{
    public const int MaxTextLength = 300;
    public const int MaxBodyLength = 8000;
    public const int MaxUrlLength = 500;
    public const int MaxMediaIdLength = 64;

    public Guid PageId { get; private set; }
    public SectionKind Kind { get; private set; }
    public string? Eyebrow { get; private set; }
    public string? Heading { get; private set; }
    public string? Subheading { get; private set; }
    public string? Body { get; private set; }

    /// <summary>Opaque media reference with no FK (a Media module id, or a legacy key the website resolves
    /// itself) — modules never share tables (ARCHITECTURE.md). Same convention as ProductMedia.</summary>
    public string? MediaId { get; private set; }

    public string? CtaLabel { get; private set; }

    /// <summary>A site-relative path or an absolute http(s) URL — see <see cref="SafeUrl"/>.</summary>
    public string? CtaUrl { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsVisible { get; private set; } = true;

    private PageSection()
    {
        // EF Core
    }

    public static PageSection Create(Guid pageId, SectionDetails details)
    {
        var section = new PageSection { Id = Guid.NewGuid(), PageId = Guard.NotEmpty(pageId, nameof(pageId)) };
        section.Update(details);
        return section;
    }

    public void Update(SectionDetails details)
    {
        if (!string.IsNullOrWhiteSpace(details.CtaUrl) && !SafeUrl.IsSafe(details.CtaUrl))
        {
            throw new BusinessRuleValidationException("The call-to-action URL must be a site path (/...) or an http(s) URL.");
        }

        Kind = details.Kind;
        Eyebrow = Clean(details.Eyebrow);
        Heading = Clean(details.Heading);
        Subheading = Clean(details.Subheading);
        Body = Clean(details.Body);
        MediaId = Clean(details.MediaId);
        CtaLabel = Clean(details.CtaLabel);
        CtaUrl = Clean(details.CtaUrl);
        DisplayOrder = details.DisplayOrder;
        IsVisible = details.IsVisible;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>The editable fields of a section, grouped so Create/Update stay readable.</summary>
public sealed record SectionDetails(
    SectionKind Kind,
    string? Eyebrow,
    string? Heading,
    string? Subheading,
    string? Body,
    string? MediaId,
    string? CtaLabel,
    string? CtaUrl,
    int DisplayOrder,
    bool IsVisible);

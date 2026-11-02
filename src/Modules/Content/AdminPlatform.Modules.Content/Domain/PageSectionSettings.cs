using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Kind-specific extras of a <see cref="PageSection"/>, stored together in one jsonb column so a new
/// extra does not need a new column. <see cref="GalleryMediaIds"/> are the secondary images (same opaque media
/// reference convention as <see cref="PageSection.MediaId"/>), in display order. <see cref="Items"/> are the rows of
/// a list section (stats, timeline, faq) or the source links of an award, in display order.</summary>
public sealed record PageSectionSettings
{
    public const int MaxGalleryMedia = 6;
    public const int MaxItems = 20;
    public const int MaxItemTextLength = 2000;

    public IReadOnlyList<string> GalleryMediaIds { get; init; } = [];

    public IReadOnlyList<PageSectionItem> Items { get; init; } = [];

    /// <summary>Trims and drops blank ids and empty items; null when nothing is left, so an empty settings object is
    /// never stored.</summary>
    public static PageSectionSettings? Create(IEnumerable<string?>? galleryMediaIds, IEnumerable<PageSectionItem?>? items = null)
    {
        var ids = (galleryMediaIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .ToList();

        if (ids.Count > MaxGalleryMedia)
        {
            throw new BusinessRuleValidationException($"A section can have at most {MaxGalleryMedia} gallery images.");
        }

        var rows = (items ?? [])
            .Select(item => item?.Cleaned())
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        if (rows.Count > MaxItems)
        {
            throw new BusinessRuleValidationException($"A section can have at most {MaxItems} items.");
        }

        return ids.Count == 0 && rows.Count == 0 ? null : new PageSectionSettings { GalleryMediaIds = ids, Items = rows };
    }

    /// <summary>Items belong to list kinds only, and each kind needs its own fields: stats a label and a value
    /// (title), timeline and faq a title and a text, award (its source links) a title and a url.</summary>
    public void EnsureValidFor(SectionKind kind)
    {
        if (Items.Count == 0)
        {
            return;
        }

        Func<PageSectionItem, bool> isComplete = kind switch
        {
            SectionKind.Stats => item => item.Label is not null && item.Title is not null,
            SectionKind.Timeline or SectionKind.Faq => item => item.Title is not null && item.Text is not null,
            SectionKind.Award => item => item.Title is not null && item.Url is not null,
            _ => throw new BusinessRuleValidationException("Only stats, timeline, faq and award sections can have items."),
        };

        if (!Items.All(isComplete))
        {
            throw new BusinessRuleValidationException(kind switch
            {
                SectionKind.Stats => "Every stats item needs a label and a value (title).",
                SectionKind.Award => "Every award item (a source link) needs a title and a url.",
                _ => $"Every {kind.ToString().ToLowerInvariant()} item needs a title and a text.",
            });
        }
    }
}

/// <summary>One row of a list section. What each field means depends on the kind: stats (label = caption, title =
/// value such as "60+"), timeline (label = year/milestone, title, text, optional image), faq (title = question,
/// text = answer). Url is an optional link (site path or http(s) URL); MediaId follows the
/// <see cref="PageSection.MediaId"/> convention.</summary>
public sealed record PageSectionItem(string? Label, string? Title, string? Text, string? MediaId, string? Url)
{
    /// <summary>Trimmed copy (blank → null); null when every field is blank. Rejects an unsafe URL.</summary>
    internal PageSectionItem? Cleaned()
    {
        var cleaned = new PageSectionItem(Clean(Label), Clean(Title), Clean(Text), Clean(MediaId), Clean(Url));

        if (cleaned is { Label: null, Title: null, Text: null, MediaId: null, Url: null })
        {
            return null;
        }

        if (cleaned.Url is not null && !SafeUrl.IsSafe(cleaned.Url))
        {
            throw new BusinessRuleValidationException("An item link must be a site path (/...) or an http(s) URL.");
        }

        return cleaned;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

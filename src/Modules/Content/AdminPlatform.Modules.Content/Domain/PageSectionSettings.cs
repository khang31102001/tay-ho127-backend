using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Kind-specific extras of a <see cref="PageSection"/>, stored together in one jsonb column so a new
/// extra does not need a new column. <see cref="GalleryMediaIds"/> are the secondary images (same opaque media
/// reference convention as <see cref="PageSection.MediaId"/>), in display order.</summary>
public sealed record PageSectionSettings
{
    public const int MaxGalleryMedia = 6;

    public IReadOnlyList<string> GalleryMediaIds { get; init; } = [];

    /// <summary>Trims and drops blank ids; null when nothing is left, so an empty settings object is never stored.</summary>
    public static PageSectionSettings? Create(IEnumerable<string?>? galleryMediaIds)
    {
        var ids = (galleryMediaIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .ToList();

        if (ids.Count > MaxGalleryMedia)
        {
            throw new BusinessRuleValidationException($"A section can have at most {MaxGalleryMedia} gallery images.");
        }

        return ids.Count == 0 ? null : new PageSectionSettings { GalleryMediaIds = ids };
    }
}

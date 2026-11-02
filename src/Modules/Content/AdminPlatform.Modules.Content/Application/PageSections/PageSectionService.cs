using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.PageSections;

public sealed class PageSectionService : IPageSectionService
{
    /// <summary>Gap between reordered positions, so a section created later can still be slotted in between.</summary>
    private const int DisplayOrderStep = 10;

    private readonly IContentDbContext _db;

    public PageSectionService(IContentDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PageSectionResponse>> ListAsync(Guid pageId, CancellationToken cancellationToken)
    {
        await EnsurePageExistsAsync(pageId, cancellationToken);

        var sections = await _db.PageSections.AsNoTracking()
            .Where(s => s.PageId == pageId)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return sections.Select(ToResponse).ToList();
    }

    public async Task<PageSectionResponse> GetByIdAsync(Guid pageId, Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(pageId, id, cancellationToken));
    }

    public async Task<PageSectionResponse> CreateAsync(Guid pageId, CreatePageSectionRequest request, CancellationToken cancellationToken)
    {
        await EnsurePageExistsAsync(pageId, cancellationToken);

        var section = PageSection.Create(pageId, ToDetails(request.SectionKind, request.Eyebrow, request.Heading, request.Subheading,
            request.Body, request.MediaId, request.CtaLabel, request.CtaUrl, request.DisplayOrder, request.IsVisible, request.Settings,
            request.SectionKey));
        await EnsureKeyIsFreeAsync(section, cancellationToken);

        _db.PageSections.Add(section);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(section);
    }

    public async Task<PageSectionResponse> UpdateAsync(Guid pageId, Guid id, UpdatePageSectionRequest request, CancellationToken cancellationToken)
    {
        var section = await FindOrThrowAsync(pageId, id, cancellationToken);

        section.Update(ToDetails(request.SectionKind, request.Eyebrow, request.Heading, request.Subheading, request.Body,
            request.MediaId, request.CtaLabel, request.CtaUrl, request.DisplayOrder, request.IsVisible, request.Settings,
            request.SectionKey));
        await EnsureKeyIsFreeAsync(section, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(section);
    }

    public async Task<IReadOnlyList<PageSectionResponse>> ReorderAsync(
        Guid pageId, ReorderPageSectionsRequest request, CancellationToken cancellationToken)
    {
        await EnsurePageExistsAsync(pageId, cancellationToken);

        var sections = await _db.PageSections.Where(s => s.PageId == pageId).ToListAsync(cancellationToken);
        var sectionIds = request.SectionIds ?? [];
        var isSameSet = sectionIds.Count == sections.Count && sectionIds.Distinct().Count() == sectionIds.Count
            && sections.All(s => sectionIds.Contains(s.Id));
        if (!isSameSet)
        {
            throw new BusinessRuleValidationException("SectionIds must list every section of the page exactly once.");
        }

        var sectionsById = sections.ToDictionary(s => s.Id);
        for (var index = 0; index < sectionIds.Count; index++)
        {
            sectionsById[sectionIds[index]].MoveTo((index + 1) * DisplayOrderStep);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return sectionIds.Select(id => ToResponse(sectionsById[id])).ToList();
    }

    public async Task DeleteAsync(Guid pageId, Guid id, CancellationToken cancellationToken)
    {
        var section = await FindOrThrowAsync(pageId, id, cancellationToken);
        _db.PageSections.Remove(section);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePageExistsAsync(Guid pageId, CancellationToken cancellationToken)
    {
        if (!await _db.Pages.AnyAsync(p => p.Id == pageId, cancellationToken))
        {
            throw new NotFoundException(nameof(Page), pageId);
        }
    }

    /// <summary>The unique index backs this up; checking first turns a duplicate key into a readable 409.</summary>
    private async Task EnsureKeyIsFreeAsync(PageSection section, CancellationToken cancellationToken)
    {
        if (section.SectionKey is null)
        {
            return;
        }

        var isTaken = await _db.PageSections.AnyAsync(
            s => s.PageId == section.PageId && s.SectionKey == section.SectionKey && s.Id != section.Id, cancellationToken);
        if (isTaken)
        {
            throw new ConflictException($"The page already has a section with key '{section.SectionKey}'.");
        }
    }

    /// <summary>A section id under the wrong page is a 404, the same as an unknown id.</summary>
    private async Task<PageSection> FindOrThrowAsync(Guid pageId, Guid id, CancellationToken cancellationToken) =>
        await _db.PageSections.SingleOrDefaultAsync(s => s.Id == id && s.PageId == pageId, cancellationToken)
            ?? throw new NotFoundException(nameof(PageSection), id);

    private static SectionDetails ToDetails(
        string sectionKind, string? eyebrow, string? heading, string? subheading, string? body, string? mediaId,
        string? ctaLabel, string? ctaUrl, int displayOrder, bool isVisible, PageSectionSettingsRequest? settings, string? sectionKey)
    {
        // The validators already checked the kind; a failed parse here would be a programming error.
        ContentWireFormat.TryParseSectionKind(sectionKind, out var kind);

        return new SectionDetails(kind, eyebrow, heading, subheading, body, mediaId, ctaLabel, ctaUrl, displayOrder, isVisible,
            PageSectionSettings.Create(settings?.GalleryMediaIds, settings?.Items?.Select(item =>
                item == null ? null : new PageSectionItem(item.Label, item.Title, item.Text, item.MediaId, item.Url))),
            sectionKey);
    }

    private static PageSectionResponse ToResponse(PageSection section) => new(
        section.Id, section.PageId, section.SectionKey, ContentWireFormat.ToWire(section.Kind), section.Eyebrow, section.Heading, section.Subheading,
        section.Body, section.MediaId, section.CtaLabel, section.CtaUrl, section.DisplayOrder, section.IsVisible,
        ToSettingsResponse(section.Settings), section.CreatedAtUtc, section.UpdatedAtUtc);

    internal static PageSectionSettingsResponse? ToSettingsResponse(PageSectionSettings? settings) =>
        settings == null
            ? null
            : new PageSectionSettingsResponse(settings.GalleryMediaIds, settings.Items
                .Select(item => new PageSectionItemResponse(item.Label, item.Title, item.Text, item.MediaId, item.Url))
                .ToList());
}

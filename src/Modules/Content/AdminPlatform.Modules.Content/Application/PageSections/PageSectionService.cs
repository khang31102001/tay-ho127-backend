using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.PageSections;

public sealed class PageSectionService : IPageSectionService
{
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
            request.Body, request.MediaId, request.CtaLabel, request.CtaUrl, request.DisplayOrder, request.IsVisible));

        _db.PageSections.Add(section);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(section);
    }

    public async Task<PageSectionResponse> UpdateAsync(Guid pageId, Guid id, UpdatePageSectionRequest request, CancellationToken cancellationToken)
    {
        var section = await FindOrThrowAsync(pageId, id, cancellationToken);

        section.Update(ToDetails(request.SectionKind, request.Eyebrow, request.Heading, request.Subheading, request.Body,
            request.MediaId, request.CtaLabel, request.CtaUrl, request.DisplayOrder, request.IsVisible));

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(section);
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

    /// <summary>A section id under the wrong page is a 404, the same as an unknown id.</summary>
    private async Task<PageSection> FindOrThrowAsync(Guid pageId, Guid id, CancellationToken cancellationToken) =>
        await _db.PageSections.SingleOrDefaultAsync(s => s.Id == id && s.PageId == pageId, cancellationToken)
            ?? throw new NotFoundException(nameof(PageSection), id);

    private static SectionDetails ToDetails(
        string sectionKind, string? eyebrow, string? heading, string? subheading, string? body, string? mediaId,
        string? ctaLabel, string? ctaUrl, int displayOrder, bool isVisible)
    {
        // The validators already checked the kind; a failed parse here would be a programming error.
        ContentWireFormat.TryParseSectionKind(sectionKind, out var kind);

        return new SectionDetails(kind, eyebrow, heading, subheading, body, mediaId, ctaLabel, ctaUrl, displayOrder, isVisible);
    }

    private static PageSectionResponse ToResponse(PageSection section) => new(
        section.Id, section.PageId, ContentWireFormat.ToWire(section.Kind), section.Eyebrow, section.Heading, section.Subheading,
        section.Body, section.MediaId, section.CtaLabel, section.CtaUrl, section.DisplayOrder, section.IsVisible,
        section.CreatedAtUtc, section.UpdatedAtUtc);
}

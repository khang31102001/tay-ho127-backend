using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.Banners;

public sealed class BannerService : IBannerService
{
    private readonly IContentDbContext _db;

    public BannerService(IContentDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<BannerResponse>> ListAsync(
        PagedRequest request, string? placement, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.Banners.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(placement))
        {
            if (!BannerWireFormat.TryParsePlacement(placement, out var parsedPlacement))
            {
                throw new BusinessRuleValidationException("Placement must be SECTIONS, ARTICLE_BANNER, HOME_HERO, HOME_PROMOTION or MENU_HERO.");
            }

            query = query.Where(b => b.Placement == parsedPlacement);
        }

        if (isActive is { } active)
        {
            query = query.Where(b => b.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, pattern) || (b.Heading != null && EF.Functions.ILike(b.Heading, pattern)));
        }

        query = request.IsDescending
            ? query.OrderByDescending(b => b.DisplayOrder).ThenByDescending(b => b.Name)
            : query.OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name);

        // Mapped after paging: Placement is an enum (no Enum.ToString() in SQL) and the sections of the page's banners
        // come from one extra query instead of one per banner.
        var page = await query.ToPagedResultAsync(request, cancellationToken);
        var sectionsByBanner = await LoadSectionRefsAsync(page.Items.Select(b => b.Id).ToList(), cancellationToken);
        var items = page.Items.Select(b => ToResponse(b, sectionsByBanner.GetValueOrDefault(b.Id) ?? [])).ToList();
        return new PagedResult<BannerResponse>(items, page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<BannerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var banner = await FindOrThrowAsync(id, cancellationToken);
        return await ToResponseAsync(banner, cancellationToken);
    }

    public async Task<BannerResponse> CreateAsync(CreateBannerRequest request, CancellationToken cancellationToken)
    {
        var sectionIds = await ValidateSectionIdsAsync(request.SectionIds, cancellationToken);
        var banner = Banner.Create(ToDetails(request.Name, request.DesktopMediaId, request.MobileMediaId, request.AltText, request.Heading,
            request.Subheading, request.CtaLabel, request.CtaUrl, request.Placement, request.StartAt, request.EndAt, request.DisplayOrder,
            request.IsActive));

        _db.Banners.Add(banner);
        foreach (var sectionId in sectionIds ?? [])
        {
            _db.BannerSectionLinks.Add(BannerSectionLink.Create(banner.Id, sectionId));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(banner, cancellationToken);
    }

    public async Task<BannerResponse> UpdateAsync(Guid id, UpdateBannerRequest request, CancellationToken cancellationToken)
    {
        var banner = await FindOrThrowAsync(id, cancellationToken);
        var sectionIds = await ValidateSectionIdsAsync(request.SectionIds, cancellationToken);

        banner.Update(ToDetails(request.Name, request.DesktopMediaId, request.MobileMediaId, request.AltText, request.Heading,
            request.Subheading, request.CtaLabel, request.CtaUrl, request.Placement, request.StartAt, request.EndAt, request.DisplayOrder,
            request.IsActive));

        // null keeps the current links; a list (even empty) replaces them.
        if (sectionIds is not null)
        {
            var current = await _db.BannerSectionLinks.Where(l => l.BannerId == id).ToListAsync(cancellationToken);
            _db.BannerSectionLinks.RemoveRange(current.Where(l => !sectionIds.Contains(l.PageSectionId)));
            foreach (var sectionId in sectionIds.Where(sectionId => current.All(l => l.PageSectionId != sectionId)))
            {
                _db.BannerSectionLinks.Add(BannerSectionLink.Create(id, sectionId));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(banner, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var banner = await FindOrThrowAsync(id, cancellationToken);
        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Banner> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Banners.SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Banner), id);

    /// <summary>Distinct ids, each an existing section of kind banner; null stays null (= keep the links).</summary>
    private async Task<List<Guid>?> ValidateSectionIdsAsync(IReadOnlyList<Guid>? sectionIds, CancellationToken cancellationToken)
    {
        if (sectionIds is null)
        {
            return null;
        }

        var distinct = sectionIds.Distinct().ToList();
        var bannerSections = await _db.PageSections.AsNoTracking()
            .Where(s => distinct.Contains(s.Id) && s.Kind == SectionKind.Banner)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var invalid = distinct.Where(id => !bannerSections.Contains(id)).ToList();
        if (invalid.Count > 0)
        {
            throw new BusinessRuleValidationException($"Not a banner section: {string.Join(", ", invalid)}.");
        }

        return distinct;
    }

    private async Task<Dictionary<Guid, List<BannerSectionRef>>> LoadSectionRefsAsync(IReadOnlyCollection<Guid> bannerIds, CancellationToken cancellationToken)
    {
        if (bannerIds.Count == 0)
        {
            return [];
        }

        var rows = await (from link in _db.BannerSectionLinks.AsNoTracking()
                          join section in _db.PageSections.AsNoTracking() on link.PageSectionId equals section.Id
                          join page in _db.Pages.AsNoTracking() on section.PageId equals page.Id
                          where bannerIds.Contains(link.BannerId)
                          orderby page.SortOrder, section.DisplayOrder
                          select new { link.BannerId, SectionId = section.Id, PageId = page.Id, PageName = page.Name, section.Heading, section.SectionKey })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.BannerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new BannerSectionRef(r.SectionId, r.PageId, r.PageName, r.Heading ?? r.SectionKey ?? "Section banner")).ToList());
    }

    private static BannerDetails ToDetails(
        string name, string? desktopMediaId, string? mobileMediaId, string altText, string? heading, string? subheading,
        string? ctaLabel, string? ctaUrl, string placement, DateTime? startAt, DateTime? endAt, int displayOrder, bool isActive)
    {
        // The validators already checked the placement; a failed parse here would be a programming error.
        BannerWireFormat.TryParsePlacement(placement, out var parsedPlacement);

        return new BannerDetails(name, desktopMediaId, mobileMediaId, altText, heading, subheading, ctaLabel, ctaUrl, parsedPlacement,
            startAt, endAt, displayOrder, isActive);
    }

    private async Task<BannerResponse> ToResponseAsync(Banner banner, CancellationToken cancellationToken)
    {
        var sectionsByBanner = await LoadSectionRefsAsync([banner.Id], cancellationToken);
        return ToResponse(banner, sectionsByBanner.GetValueOrDefault(banner.Id) ?? []);
    }

    internal static BannerResponse ToResponse(Banner banner, IReadOnlyList<BannerSectionRef> sections) => new(
        banner.Id, banner.Name, banner.DesktopMediaId, banner.MobileMediaId, banner.AltText, banner.Heading, banner.Subheading,
        banner.CtaLabel, banner.CtaUrl, BannerWireFormat.ToWire(banner.Placement), banner.StartAtUtc, banner.EndAtUtc,
        banner.DisplayOrder, banner.IsActive, banner.CreatedAtUtc, banner.UpdatedAtUtc, sections);
}

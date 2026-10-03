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
                throw new BusinessRuleValidationException("Placement must be HOME_HERO, HOME_PROMOTION, MENU_HERO or ARTICLE_BANNER.");
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

        var projected = query.Select(b => new BannerResponse(
            b.Id, b.Name, b.DesktopMediaId, b.MobileMediaId, b.AltText, b.Heading, b.Subheading, b.CtaLabel, b.CtaUrl,
            b.Placement == BannerPlacement.HomeHero ? "HOME_HERO"
            : b.Placement == BannerPlacement.HomePromotion ? "HOME_PROMOTION"
            : b.Placement == BannerPlacement.MenuHero ? "MENU_HERO" : "ARTICLE_BANNER",
            b.StartAtUtc, b.EndAtUtc, b.DisplayOrder, b.IsActive, b.CreatedAtUtc, b.UpdatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<BannerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<BannerResponse> CreateAsync(CreateBannerRequest request, CancellationToken cancellationToken)
    {
        var banner = Banner.Create(ToDetails(request.Name, request.DesktopMediaId, request.MobileMediaId, request.AltText, request.Heading,
            request.Subheading, request.CtaLabel, request.CtaUrl, request.Placement, request.StartAt, request.EndAt, request.DisplayOrder,
            request.IsActive));

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(banner);
    }

    public async Task<BannerResponse> UpdateAsync(Guid id, UpdateBannerRequest request, CancellationToken cancellationToken)
    {
        var banner = await FindOrThrowAsync(id, cancellationToken);

        banner.Update(ToDetails(request.Name, request.DesktopMediaId, request.MobileMediaId, request.AltText, request.Heading,
            request.Subheading, request.CtaLabel, request.CtaUrl, request.Placement, request.StartAt, request.EndAt, request.DisplayOrder,
            request.IsActive));

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(banner);
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

    private static BannerDetails ToDetails(
        string name, string? desktopMediaId, string? mobileMediaId, string altText, string? heading, string? subheading,
        string? ctaLabel, string? ctaUrl, string placement, DateTime? startAt, DateTime? endAt, int displayOrder, bool isActive)
    {
        // The validators already checked the placement; a failed parse here would be a programming error.
        BannerWireFormat.TryParsePlacement(placement, out var parsedPlacement);

        return new BannerDetails(name, desktopMediaId, mobileMediaId, altText, heading, subheading, ctaLabel, ctaUrl, parsedPlacement,
            startAt, endAt, displayOrder, isActive);
    }

    internal static BannerResponse ToResponse(Banner banner) => new(
        banner.Id, banner.Name, banner.DesktopMediaId, banner.MobileMediaId, banner.AltText, banner.Heading, banner.Subheading,
        banner.CtaLabel, banner.CtaUrl, BannerWireFormat.ToWire(banner.Placement), banner.StartAtUtc, banner.EndAtUtc,
        banner.DisplayOrder, banner.IsActive, banner.CreatedAtUtc, banner.UpdatedAtUtc);
}

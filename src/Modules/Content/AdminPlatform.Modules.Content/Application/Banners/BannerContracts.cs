using AdminPlatform.Modules.Content.Domain;

namespace AdminPlatform.Modules.Content.Application.Banners;

/// <summary>Placement: "SECTIONS" (shown in the banner sections listed in SectionIds) | "ARTICLE_BANNER" | the legacy "HOME_HERO" |
/// "HOME_PROMOTION" | "MENU_HERO" (case-insensitive). SectionIds = the banner sections (PageSection of kind banner) the banner is
/// shown in; omitted or null keeps the current links on update, an empty list unlinks it everywhere.
/// StartAt/EndAt are optional UTC instants (no bound = unlimited). CtaUrl must be a site path or an http(s) URL.</summary>
public sealed record CreateBannerRequest(
    string Name,
    string? DesktopMediaId,
    string? MobileMediaId,
    string AltText,
    string? Heading,
    string? Subheading,
    string? CtaLabel,
    string? CtaUrl,
    string Placement,
    DateTime? StartAt,
    DateTime? EndAt,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<Guid>? SectionIds = null);

public sealed record UpdateBannerRequest(
    string Name,
    string? DesktopMediaId,
    string? MobileMediaId,
    string AltText,
    string? Heading,
    string? Subheading,
    string? CtaLabel,
    string? CtaUrl,
    string Placement,
    DateTime? StartAt,
    DateTime? EndAt,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<Guid>? SectionIds = null);

public sealed record BannerResponse(
    Guid Id,
    string Name,
    string? DesktopMediaId,
    string? MobileMediaId,
    string AltText,
    string? Heading,
    string? Subheading,
    string? CtaLabel,
    string? CtaUrl,
    string Placement,
    DateTime? StartAt,
    DateTime? EndAt,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<BannerSectionRef>? Sections = null);

/// <summary>A banner section a banner is shown in: the page it belongs to and a readable label of the section.</summary>
public sealed record BannerSectionRef(Guid SectionId, Guid PageId, string PageName, string SectionLabel);

/// <summary>Maps <see cref="BannerPlacement"/> to/from the SCREAMING_SNAKE names used on the wire.</summary>
public static class BannerWireFormat
{
    public static string ToWire(BannerPlacement placement) => placement switch
    {
        BannerPlacement.HomeHero => "HOME_HERO",
        BannerPlacement.HomePromotion => "HOME_PROMOTION",
        BannerPlacement.MenuHero => "MENU_HERO",
        BannerPlacement.ArticleBanner => "ARTICLE_BANNER",
        BannerPlacement.Sections => "SECTIONS",
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    public static bool TryParsePlacement(string? value, out BannerPlacement placement)
    {
        foreach (var candidate in Enum.GetValues<BannerPlacement>())
        {
            if (string.Equals(ToWire(candidate), value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                placement = candidate;
                return true;
            }
        }

        placement = default;
        return false;
    }
}

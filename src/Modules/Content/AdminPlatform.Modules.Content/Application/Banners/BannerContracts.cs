using AdminPlatform.Modules.Content.Domain;

namespace AdminPlatform.Modules.Content.Application.Banners;

/// <summary>Placement: "HOME_HERO" | "HOME_PROMOTION" | "MENU_HERO" | "ARTICLE_BANNER" (case-insensitive).
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
    bool IsActive);

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
    bool IsActive);

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
    DateTime? UpdatedAtUtc);

/// <summary>Maps <see cref="BannerPlacement"/> to/from the SCREAMING_SNAKE names used on the wire.</summary>
public static class BannerWireFormat
{
    public static string ToWire(BannerPlacement placement) => placement switch
    {
        BannerPlacement.HomeHero => "HOME_HERO",
        BannerPlacement.HomePromotion => "HOME_PROMOTION",
        BannerPlacement.MenuHero => "MENU_HERO",
        BannerPlacement.ArticleBanner => "ARTICLE_BANNER",
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

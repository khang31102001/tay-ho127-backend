namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Where on the website a banner is shown. <see cref="Sections"/> (the normal case) means the banner is shown
/// wherever it is linked to a banner section of a page (<see cref="BannerSectionLink"/>). The other values are
/// the legacy fixed placements; only <see cref="ArticleBanner"/> is still read by the website directly (the article
/// page is a template, not a page with sections). Adding a placement means adding a value here (and its wire name in
/// <c>BannerWireFormat</c>).</summary>
public enum BannerPlacement
{
    HomeHero,
    HomePromotion,
    MenuHero,
    ArticleBanner,
    Sections,
}

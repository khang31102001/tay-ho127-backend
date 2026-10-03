namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Where on the website a banner is shown. A banner is not tied to a page: the website asks for
/// "the active banners of placement X". Adding a placement means adding a value here (and its wire name
/// in <c>BannerWireFormat</c>).</summary>
public enum BannerPlacement
{
    HomeHero,
    HomePromotion,
    MenuHero,
    ArticleBanner,
}

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>A banner shown in a banner section of a page. Many-to-many: one banner can be linked to several sections
/// (on several pages) and a section can show several banners (a carousel). Banners in a section are shown by their
/// own DisplayOrder. Links are removed with either side (DB cascade); the banner/section itself is never deleted by it.</summary>
public sealed class BannerSectionLink
{
    public Guid BannerId { get; private set; }
    public Guid PageSectionId { get; private set; }

    private BannerSectionLink()
    {
        // EF Core
    }

    public static BannerSectionLink Create(Guid bannerId, Guid pageSectionId) => new() { BannerId = bannerId, PageSectionId = pageSectionId };
}

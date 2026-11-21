using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.Banners;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Content;

/// <summary>Banners are shown through banner sections of pages (many-to-many): linking, validation, and what the website reads.</summary>
public class BannerSectionLinkTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private static IContentDbContext NewDb() =>
        new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Guid PageId, Guid BannerSectionId, Guid HeroSectionId)> SeedPageAsync(IContentDbContext db, string pageKey = "home")
    {
        var page = Page.Create("Trang chủ", pageKey == "home" ? "/" : "/" + pageKey, PublishStatus.Published, Now, pageKey);
        var banner = PageSection.Create(page.Id, new SectionDetails(SectionKind.Banner, null, "Ưu đãi", null, null, null, null, null, 1, true));
        var hero = PageSection.Create(page.Id, new SectionDetails(SectionKind.Hero, null, "Hero", null, null, null, null, null, 2, true));
        db.Pages.Add(page);
        db.PageSections.AddRange(banner, hero);
        await db.SaveChangesAsync(None);
        return (page.Id, banner.Id, hero.Id);
    }

    private static CreateBannerRequest NewBanner(string name, IReadOnlyList<Guid>? sectionIds, int order = 1, bool isActive = true, DateTime? end = null) =>
        new(name, "media-a", null, "Alt", null, null, null, null, "SECTIONS", null, end, order, isActive, sectionIds);

    [Fact]
    public async Task Create_links_the_banner_to_its_banner_sections_and_returns_them()
    {
        var db = NewDb();
        var (pageId, bannerSectionId, _) = await SeedPageAsync(db);

        var created = await new BannerService(db).CreateAsync(NewBanner("Khuyến mãi", [bannerSectionId]), None);

        var section = Assert.Single(created.Sections!);
        Assert.Equal(bannerSectionId, section.SectionId);
        Assert.Equal(pageId, section.PageId);
        Assert.Equal("Trang chủ", section.PageName);
        Assert.Equal("Ưu đãi", section.SectionLabel);
        Assert.Equal("SECTIONS", created.Placement);
    }

    [Fact]
    public async Task Create_rejects_a_section_that_is_not_a_banner_section()
    {
        var db = NewDb();
        var (_, _, heroSectionId) = await SeedPageAsync(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => new BannerService(db).CreateAsync(NewBanner("X", [heroSectionId]), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => new BannerService(db).CreateAsync(NewBanner("Y", [Guid.NewGuid()]), None));
        Assert.Empty(db.Banners);
    }

    [Fact]
    public async Task One_banner_can_be_shown_in_sections_of_several_pages()
    {
        var db = NewDb();
        var (_, first, _) = await SeedPageAsync(db, "home");
        var (_, second, _) = await SeedPageAsync(db, "thuc-don");

        var created = await new BannerService(db).CreateAsync(NewBanner("Dùng chung", [first, second]), None);

        Assert.Equal(2, created.Sections!.Count);
    }

    [Fact]
    public async Task Update_keeps_links_when_section_ids_is_null_and_replaces_them_otherwise()
    {
        var db = NewDb();
        var (_, first, _) = await SeedPageAsync(db, "home");
        var (_, second, _) = await SeedPageAsync(db, "thuc-don");
        var sut = new BannerService(db);
        var created = await sut.CreateAsync(NewBanner("B", [first]), None);

        UpdateBannerRequest Update(IReadOnlyList<Guid>? ids) =>
            new("B", "media-a", null, "Alt", null, null, null, null, "SECTIONS", null, null, 1, true, ids);

        var kept = await sut.UpdateAsync(created.Id, Update(null), None);
        Assert.Equal(first, Assert.Single(kept.Sections!).SectionId);

        var moved = await sut.UpdateAsync(created.Id, Update([second]), None);
        Assert.Equal(second, Assert.Single(moved.Sections!).SectionId);

        var cleared = await sut.UpdateAsync(created.Id, Update([]), None);
        Assert.Empty(cleared.Sections!);
    }

    [Fact]
    public async Task Public_page_sections_carry_the_live_banners_of_a_banner_section_only()
    {
        var db = NewDb();
        var (_, bannerSectionId, _) = await SeedPageAsync(db);
        var admin = new BannerService(db);
        await admin.CreateAsync(NewBanner("Thứ hai", [bannerSectionId], order: 2), None);
        await admin.CreateAsync(NewBanner("Thứ nhất", [bannerSectionId], order: 1), None);
        await admin.CreateAsync(NewBanner("Tắt", [bannerSectionId], order: 3, isActive: false), None);
        await admin.CreateAsync(NewBanner("Hết hạn", [bannerSectionId], order: 4, end: Now.AddDays(-1)), None);

        var sections = await new PublicContentService(db, new FixedClock(Now)).ListPageSectionsAsync(null, "home", None);

        var banners = sections.Single(s => s.SectionKind == "banner").Banners!;
        Assert.Equal(["Thứ nhất", "Thứ hai"], banners.Select(b => b.Name));
        Assert.Null(sections.Single(s => s.SectionKind == "hero").Banners);
    }

    [Fact]
    public async Task Deleting_a_banner_does_not_remove_its_section()
    {
        var db = NewDb();
        var (_, bannerSectionId, _) = await SeedPageAsync(db);
        var sut = new BannerService(db);
        var created = await sut.CreateAsync(NewBanner("B", [bannerSectionId]), None);

        await sut.DeleteAsync(created.Id, None);

        Assert.Single(db.PageSections, s => s.Id == bannerSectionId);
    }
}

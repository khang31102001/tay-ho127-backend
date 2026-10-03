using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.Banners;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Content;

public class BannerTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private static IContentDbContext NewDb() =>
        new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateBannerRequest NewBanner(
        string name = "Khuyến mãi", string placement = "HOME_PROMOTION", string? ctaUrl = "/thuc-don", int order = 1,
        bool isActive = true, DateTime? start = null, DateTime? end = null) =>
        new(name, "media-a", "media-b", "Ảnh banner", "Tiêu đề", null, "Xem ngay", ctaUrl, placement, start, end, order, isActive);

    private static BannerDetails Details(string? ctaUrl = null, DateTime? start = null, DateTime? end = null, bool isActive = true) =>
        new("B", null, null, "Alt", null, null, null, ctaUrl, BannerPlacement.HomeHero, start, end, 1, isActive);

    // ---------- Domain ----------

    [Theory]
    [InlineData("/thuc-don", true)]
    [InlineData("/menu?x=1#top", true)]
    [InlineData("https://tayho127.vn/uu-dai", true)]
    [InlineData("http://example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html;base64,AAAA", false)]
    [InlineData("//evil.com/x", false)]
    [InlineData("/\\evil.com", false)]
    [InlineData("thuc-don", false)]
    [InlineData("ftp://host/file", false)]
    public void Cta_url_must_be_a_site_path_or_http_url(string url, bool expectedSafe)
    {
        Assert.Equal(expectedSafe, Banner.IsSafeUrl(url));
        if (expectedSafe)
        {
            Assert.Equal(url, Banner.Create(Details(ctaUrl: url)).CtaUrl);
        }
        else
        {
            Assert.Throws<BusinessRuleValidationException>(() => Banner.Create(Details(ctaUrl: url)));
        }
    }

    [Fact]
    public void Blank_optional_fields_are_stored_as_null_and_alt_text_is_required()
    {
        var banner = Banner.Create(Details(ctaUrl: "  "));
        Assert.Null(banner.CtaUrl);
        Assert.Throws<BusinessRuleValidationException>(() => Banner.Create(Details() with { AltText = " " }));
        Assert.Throws<BusinessRuleValidationException>(() => Banner.Create(Details() with { Name = "" }));
    }

    [Fact]
    public void End_date_must_be_after_start_date_and_unspecified_dates_are_utc()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Banner.Create(Details(start: Now, end: Now)));
        var banner = Banner.Create(Details(end: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified)));
        Assert.Equal(DateTimeKind.Utc, banner.EndAtUtc!.Value.Kind);
    }

    [Fact]
    public void A_banner_is_live_only_when_active_and_inside_its_window()
    {
        Assert.True(Banner.Create(Details()).IsLiveAt(Now));
        Assert.False(Banner.Create(Details(isActive: false)).IsLiveAt(Now));
        Assert.False(Banner.Create(Details(start: Now.AddDays(1))).IsLiveAt(Now));
        Assert.False(Banner.Create(Details(end: Now.AddDays(-1))).IsLiveAt(Now));
        Assert.True(Banner.Create(Details(start: Now, end: Now.AddHours(1))).IsLiveAt(Now));
    }

    // ---------- Admin service ----------

    [Fact]
    public async Task Create_update_get_delete_round_trip_with_wire_placement()
    {
        var sut = new BannerService(NewDb());

        var created = await sut.CreateAsync(NewBanner(placement: "home_hero"), None);
        Assert.Equal("HOME_HERO", created.Placement);
        Assert.Equal("media-a", created.DesktopMediaId);

        var updated = await sut.UpdateAsync(created.Id,
            new UpdateBannerRequest("Đổi tên", null, null, "Alt mới", null, "Phụ đề", null, null, "MENU_HERO", null, null, 5, false), None);
        Assert.Equal("MENU_HERO", updated.Placement);
        Assert.False(updated.IsActive);
        Assert.Null(updated.DesktopMediaId);
        Assert.Equal(created.Id, (await sut.GetByIdAsync(created.Id, None)).Id);

        await sut.DeleteAsync(created.Id, None);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(created.Id, None));
    }

    // ---------- Public read model ----------

    [Fact]
    public async Task Public_banners_only_include_live_banners_of_the_placement_in_display_order()
    {
        var db = NewDb();
        var admin = new BannerService(db);
        await admin.CreateAsync(NewBanner("Thứ hai", "HOME_HERO", order: 2), None);
        await admin.CreateAsync(NewBanner("Thứ nhất", "HOME_HERO", order: 1), None);
        await admin.CreateAsync(NewBanner("Đã tắt", "HOME_HERO", isActive: false), None);
        await admin.CreateAsync(NewBanner("Chưa tới", "HOME_HERO", start: Now.AddDays(1)), None);
        await admin.CreateAsync(NewBanner("Hết hạn", "HOME_HERO", end: Now.AddDays(-1)), None);
        await admin.CreateAsync(NewBanner("Vị trí khác", "MENU_HERO"), None);

        var sut = new PublicContentService(db, new FixedClock(Now));
        var hero = await sut.ListBannersAsync("home_hero", None);

        Assert.Equal(["Thứ nhất", "Thứ hai"], hero.Select(b => b.Name));
        Assert.All(hero, b => Assert.Equal("HOME_HERO", b.Placement));
        Assert.Equal(3, (await sut.ListBannersAsync(null, None)).Count);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ListBannersAsync("NOWHERE", None));
    }
}

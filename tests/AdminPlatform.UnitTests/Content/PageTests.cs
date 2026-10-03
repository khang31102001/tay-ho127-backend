using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.PageSections;
using AdminPlatform.Modules.Content.Application.Pages;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Content;

public class PageTests
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

    private static PageService Pages(IContentDbContext db) => new(db, new FixedClock(Now));

    private static CreatePageSectionRequest NewSection(
        string kind = "introduction", string? ctaUrl = "/thuc-don", int order = 1, bool visible = true, string? body = "Nội dung") =>
        new(kind, "Eyebrow", "Tiêu đề", "Phụ đề", body, "media-a", "Xem", ctaUrl, order, visible);

    // ---------- Domain ----------

    [Theory]
    [InlineData("/", true)]
    [InlineData("/thuc-don", true)]
    [InlineData("/ve-chung-toi/lich-su", true)]
    [InlineData("/trang-2", true)]
    [InlineData("thuc-don", false)]
    [InlineData("", false)]
    [InlineData("/Thuc-Don", false)]
    [InlineData("/thuc don", false)]
    [InlineData("/thuc-don/", false)]
    [InlineData("//evil.com", false)]
    [InlineData("/-bad", false)]
    [InlineData("/bad--double", false)]
    public void Page_path_is_the_site_root_or_lowercase_hyphenated_segments(string path, bool expected)
    {
        Assert.Equal(expected, Page.IsValidPath(path));
    }

    [Fact]
    public void Path_can_be_derived_from_a_vietnamese_name()
    {
        Assert.Equal("/ve-chung-toi", Page.PathFromName("Về chúng tôi"));
        Assert.Null(Page.PathFromName("!!!"));
    }

    [Fact]
    public void PublishedAt_is_set_on_first_publish_and_kept_afterwards()
    {
        var page = Page.Create("Trang chủ", "/", PublishStatus.Draft, Now);
        Assert.Null(page.PublishedAtUtc);

        page.Update("Trang chủ", "/", PublishStatus.Published, Now);
        Assert.Equal(Now, page.PublishedAtUtc);

        page.Update("Trang chủ", "/", PublishStatus.Draft, Now.AddDays(3));
        page.Update("Trang chủ", "/", PublishStatus.Published, Now.AddDays(9));
        Assert.Equal(Now, page.PublishedAtUtc);
    }

    [Fact]
    public void A_page_needs_a_name_and_valid_path()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Page.Create(" ", "/x", PublishStatus.Draft, Now));
        Assert.Throws<BusinessRuleValidationException>(() => Page.Create("X", "x", PublishStatus.Draft, Now));
    }

    [Fact]
    public void Section_cta_url_must_be_safe_and_blank_fields_become_null()
    {
        var pageId = Guid.NewGuid();
        var details = new SectionDetails(SectionKind.Hero, " ", "H", null, "", null, null, null, 1, true);
        var section = PageSection.Create(pageId, details);
        Assert.Null(section.Eyebrow);
        Assert.Null(section.Body);
        Assert.Equal("H", section.Heading);

        Assert.Throws<BusinessRuleValidationException>(() => PageSection.Create(pageId, details with { CtaUrl = "javascript:alert(1)" }));
        Assert.Throws<BusinessRuleValidationException>(() => PageSection.Create(Guid.Empty, details));
        Assert.Equal("/menu", PageSection.Create(pageId, details with { CtaUrl = "/menu" }).CtaUrl);
    }

    // ---------- Page service ----------

    [Fact]
    public async Task Path_is_generated_unique_and_explicit_duplicates_conflict()
    {
        var sut = Pages(NewDb());

        var first = await sut.CreateAsync(new CreatePageRequest("Về chúng tôi", null, "draft"), None);
        var second = await sut.CreateAsync(new CreatePageRequest("Về chúng tôi", null, "draft"), None);
        Assert.Equal("/ve-chung-toi", first.Slug);
        Assert.Equal("/ve-chung-toi-2", second.Slug);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(new CreatePageRequest("Khác", "/ve-chung-toi", "draft"), None));

        var kept = await sut.UpdateAsync(first.Id, new UpdatePageRequest("Đổi tên", null, "published"), None);
        Assert.Equal("/ve-chung-toi", kept.Slug);
        Assert.Equal("published", kept.Status);
        Assert.NotNull(kept.PublishedAt);

        var sameExplicit = await sut.UpdateAsync(first.Id, new UpdatePageRequest("Đổi tên", "/ve-chung-toi", "published"), None);
        Assert.Equal("/ve-chung-toi", sameExplicit.Slug);
    }

    [Fact]
    public async Task Home_page_can_use_the_root_path_once()
    {
        var sut = Pages(NewDb());
        var home = await sut.CreateAsync(new CreatePageRequest("Trang chủ", "/", "published"), None);
        Assert.Equal("/", home.Slug);
        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(new CreatePageRequest("Trang chủ 2", "/", "draft"), None));
    }

    // ---------- Section service ----------

    [Fact]
    public async Task Sections_are_scoped_to_their_page_and_ordered()
    {
        var db = NewDb();
        var pages = Pages(db);
        var home = await pages.CreateAsync(new CreatePageRequest("Trang chủ", "/", "draft"), None);
        var menu = await pages.CreateAsync(new CreatePageRequest("Thực đơn", "/thuc-don", "draft"), None);
        var sut = new PageSectionService(db);

        var second = await sut.CreateAsync(home.Id, NewSection("cta", order: 2), None);
        var first = await sut.CreateAsync(home.Id, NewSection("HERO", order: 1), None);
        await sut.CreateAsync(menu.Id, NewSection(order: 1), None);

        Assert.Equal("hero", first.SectionKind);
        Assert.Equal([first.Id, second.Id], (await sut.ListAsync(home.Id, None)).Select(s => s.Id));
        Assert.Single(await sut.ListAsync(menu.Id, None));

        // A section id under the wrong page is a 404, and so is a page that does not exist.
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(menu.Id, first.Id, None));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ListAsync(Guid.NewGuid(), None));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateAsync(Guid.NewGuid(), NewSection(), None));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteAsync(menu.Id, first.Id, None));
    }

    [Fact]
    public async Task Section_update_and_delete_work_and_unsafe_cta_is_rejected()
    {
        var db = NewDb();
        var page = await Pages(db).CreateAsync(new CreatePageRequest("Trang chủ", "/", "draft"), None);
        var sut = new PageSectionService(db);
        var created = await sut.CreateAsync(page.Id, NewSection(), None);

        var updated = await sut.UpdateAsync(page.Id, created.Id,
            new UpdatePageSectionRequest("promotion", null, "Mới", null, null, null, null, "https://tayho127.vn", 7, false), None);
        Assert.Equal("promotion", updated.SectionKind);
        Assert.False(updated.IsVisible);
        Assert.Null(updated.Eyebrow);
        Assert.Equal(7, updated.DisplayOrder);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UpdateAsync(page.Id, created.Id, new UpdatePageSectionRequest("hero", null, null, null, null, null, null, "javascript:x", 1, true), None));

        await sut.DeleteAsync(page.Id, created.Id, None);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(page.Id, created.Id, None));
    }

    // ---------- Public read model ----------

    [Fact]
    public async Task Public_pages_only_list_published_pages_by_path()
    {
        var db = NewDb();
        var pages = Pages(db);
        await pages.CreateAsync(new CreatePageRequest("Thực đơn", "/thuc-don", "published"), None);
        await pages.CreateAsync(new CreatePageRequest("Trang chủ", "/", "published"), None);
        await pages.CreateAsync(new CreatePageRequest("Nháp", "/nhap", "draft"), None);
        await pages.CreateAsync(new CreatePageRequest("Lưu trữ", "/luu-tru", "archived"), None);

        var list = await new PublicContentService(db, new FixedClock(Now)).ListPublishedPagesAsync(None);

        Assert.Equal(["/", "/thuc-don"], list.Select(p => p.Slug));
    }
}

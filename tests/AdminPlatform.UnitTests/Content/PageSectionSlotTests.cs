using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.PageSections;
using AdminPlatform.Modules.Content.Application.Pages;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.UnitTests.Content;

/// <summary>Page and section keys (website pages and slots), list items, admin ordering and the slot seeder.</summary>
public class PageSectionSlotTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static ContentDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Guid> AddPageAsync(IContentDbContext db, string slug = "/")
    {
        var page = Page.Create("Trang", slug, PublishStatus.Published, Now);
        db.Pages.Add(page);
        await db.SaveChangesAsync(None);
        return page.Id;
    }

    private static CreatePageSectionRequest NewSection(
        string? key, string kind = "hero", int order = 1, PageSectionSettingsRequest? settings = null) =>
        new(kind, null, "Tiêu đề", null, null, null, null, null, order, true, settings, key);

    private static PageSectionSettingsRequest Items(params PageSectionItemRequest[] items) => new(null, items);

    // ---------- Section key ----------

    [Theory]
    [InlineData(null, true)]
    [InlineData("hero", true)]
    [InlineData("signature-dish", true)]
    [InlineData("faq2", true)]
    [InlineData("Hero", false)]
    [InlineData("signature_dish", false)]
    [InlineData("-hero", false)]
    [InlineData("hero-", false)]
    [InlineData("a b", false)]
    public void IsValidKey_accepts_lowercase_words_joined_by_hyphens(string? key, bool expected)
    {
        Assert.Equal(expected, PageSection.IsValidKey(key));
    }

    [Fact]
    public void Blank_key_is_stored_as_null_and_an_invalid_key_is_rejected()
    {
        var section = PageSection.Create(Guid.NewGuid(), new SectionDetails(SectionKind.Hero, null, "H", null, null, null, null, null, 1, true,
            SectionKey: "  "));
        Assert.Null(section.SectionKey);

        Assert.Throws<BusinessRuleValidationException>(() => PageSection.Create(Guid.NewGuid(),
            new SectionDetails(SectionKind.Hero, null, "H", null, null, null, null, null, 1, true, SectionKey: "Not Valid")));
    }

    [Fact]
    public async Task A_key_is_unique_per_page_but_may_repeat_on_another_page()
    {
        await using var db = NewDb();
        var home = await AddPageAsync(db);
        var menu = await AddPageAsync(db, "/thuc-don");
        var sut = new PageSectionService(db);

        var hero = await sut.CreateAsync(home, NewSection("hero"), None);
        Assert.Equal("hero", hero.SectionKey);
        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(home, NewSection("hero", order: 2), None));
        await sut.CreateAsync(menu, NewSection("hero"), None);

        // Updating a section with its own key is not a conflict; taking another section's key is.
        var story = await sut.CreateAsync(home, NewSection("story", "story", 2), None);
        await sut.UpdateAsync(home, hero.Id, new UpdatePageSectionRequest("hero", null, "Mới", null, null, null, null, null, 1, true, null, "hero"), None);
        await Assert.ThrowsAsync<ConflictException>(() => sut.UpdateAsync(home, story.Id,
            new UpdatePageSectionRequest("story", null, "S", null, null, null, null, null, 2, true, null, "hero"), None));

        // Free sections (no key) never conflict.
        await sut.CreateAsync(home, NewSection(null, "cta", 3), None);
        await sut.CreateAsync(home, NewSection(null, "cta", 4), None);
    }

    // ---------- Items ----------

    [Fact]
    public async Task Stats_items_need_a_label_and_a_value_and_blank_rows_are_dropped()
    {
        await using var db = NewDb();
        var home = await AddPageAsync(db);
        var sut = new PageSectionService(db);

        var stats = await sut.CreateAsync(home, NewSection("stats", "stats", settings: Items(
            new PageSectionItemRequest(" Năm kinh nghiệm ", "60+", null, null, null),
            new PageSectionItemRequest(" ", null, "", null, null))), None);
        var item = Assert.Single(stats.Settings!.Items);
        Assert.Equal(("Năm kinh nghiệm", "60+"), (item.Label, item.Title));

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(home, NewSection("stats-2", "stats", settings: Items(
            new PageSectionItemRequest("Món ăn", null, null, null, null))), None));
    }

    [Fact]
    public async Task Timeline_and_faq_items_need_a_title_and_a_text()
    {
        await using var db = NewDb();
        var about = await AddPageAsync(db, "/gioi-thieu");
        var sut = new PageSectionService(db);

        var timeline = await sut.CreateAsync(about, NewSection("timeline", "timeline", settings: Items(
            new PageSectionItemRequest("1961", "Tiệm bánh cuốn", "Bà Cà mở tiệm…", "media-a", null))), None);
        Assert.Equal("1961", Assert.Single(timeline.Settings!.Items).Label);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(about, NewSection("faq", "faq", settings: Items(
            new PageSectionItemRequest(null, "Quán mở cửa mấy giờ?", null, null, null))), None));
    }

    [Fact]
    public async Task Items_are_rejected_on_other_kinds_and_on_unsafe_links()
    {
        await using var db = NewDb();
        var home = await AddPageAsync(db);
        var sut = new PageSectionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(home, NewSection("hero", "hero", settings: Items(
            new PageSectionItemRequest("a", "b", "c", null, null))), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(home, NewSection("faq", "faq", settings: Items(
            new PageSectionItemRequest(null, "Hỏi", "Đáp", null, "javascript:alert(1)"))), None));
        Assert.Throws<BusinessRuleValidationException>(() => PageSectionSettings.Create(null,
            Enumerable.Range(1, PageSectionSettings.MaxItems + 1).Select(i => (PageSectionItem?)new PageSectionItem("l", $"{i}", null, null, null))));
    }

    // ---------- Ordering ----------

    [Fact]
    public async Task Reorder_renumbers_every_section_in_the_given_order()
    {
        await using var db = NewDb();
        var home = await AddPageAsync(db);
        var sut = new PageSectionService(db);
        var hero = await sut.CreateAsync(home, NewSection("hero", order: 1), None);
        var story = await sut.CreateAsync(home, NewSection("story", "story", 2), None);
        var award = await sut.CreateAsync(home, NewSection("award", "award", 3), None);

        var reordered = await sut.ReorderAsync(home, new ReorderPageSectionsRequest([award.Id, hero.Id, story.Id]), None);

        Assert.Equal([award.Id, hero.Id, story.Id], reordered.Select(s => s.Id));
        Assert.Equal([10, 20, 30], reordered.Select(s => s.DisplayOrder));
        Assert.Equal([award.Id, hero.Id, story.Id], (await sut.ListAsync(home, None)).Select(s => s.Id));
    }

    [Fact]
    public async Task Reorder_needs_exactly_the_page_sections()
    {
        await using var db = NewDb();
        var home = await AddPageAsync(db);
        var other = await AddPageAsync(db, "/lien-he");
        var sut = new PageSectionService(db);
        var hero = await sut.CreateAsync(home, NewSection("hero"), None);
        var story = await sut.CreateAsync(home, NewSection("story", "story", 2), None);
        var foreign = await sut.CreateAsync(other, NewSection("hero"), None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ReorderAsync(home, new([hero.Id]), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ReorderAsync(home, new([hero.Id, hero.Id]), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ReorderAsync(home, new([hero.Id, foreign.Id]), None));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ReorderAsync(Guid.NewGuid(), new([story.Id]), None));
    }

    // ---------- Page key ----------

    private sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    [Fact]
    public async Task Page_key_is_unique_kept_when_omitted_on_update_and_finds_the_public_sections()
    {
        await using var db = NewDb();
        var pages = new PageService(db, new FixedClock(Now));

        var about = await pages.CreateAsync(new CreatePageRequest("Giới thiệu", "/gioi-thieu", "published", " gioi-thieu ", 20), None);
        Assert.Equal(("gioi-thieu", 20), (about.PageKey, about.SortOrder));
        await Assert.ThrowsAsync<ConflictException>(() => pages.CreateAsync(new CreatePageRequest("Khác", "/khac", "draft", "gioi-thieu"), None));
        Assert.Throws<BusinessRuleValidationException>(() => Page.Create("X", "/x", PublishStatus.Draft, Now, "Gioi Thieu"));

        // An older client that does not send the key or the order keeps both.
        var renamed = await pages.UpdateAsync(about.Id, new UpdatePageRequest("Về chúng tôi", null, "published"), None);
        Assert.Equal(("Về chúng tôi", "gioi-thieu", 20), (renamed.Name, renamed.PageKey, renamed.SortOrder));

        await new PageSectionService(db).CreateAsync(about.Id, NewSection("hero"), None);
        var publicContent = new PublicContentService(db, new FixedClock(Now));
        var byKey = await publicContent.ListPageSectionsAsync(null, "gioi-thieu", None);
        Assert.Equal("hero", Assert.Single(byKey).SectionKey);
        Assert.Single(await publicContent.ListPageSectionsAsync("/gioi-thieu", null, None));
        await Assert.ThrowsAsync<NotFoundException>(() => publicContent.ListPageSectionsAsync(null, "khong-co", None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => publicContent.ListPageSectionsAsync(null, " ", None));
    }

    // ---------- Seeder ----------

    [Fact]
    public async Task Seeder_fills_only_empty_slots_and_never_changes_existing_sections()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddDbContext<ContentDbContext>(o => o.UseInMemoryDatabase(databaseName))
            .AddScoped<IContentDbContext>(sp => sp.GetRequiredService<ContentDbContext>())
            .BuildServiceProvider();

        // A database already in use: the home page exists with an admin-edited hero, and nothing else.
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            var home = await AddPageAsync(db);
            db.PageSections.Add(PageSection.Create(home, new SectionDetails(SectionKind.Hero, null, "Admin đã sửa", null, null, null, null,
                null, 99, false, SectionKey: "hero")));
            await db.SaveChangesAsync(None);
        }

        using (var scope = services.CreateScope())
        {
            await ContentSeeder.SeedAsync(scope.ServiceProvider, None);
            await ContentSeeder.SeedAsync(scope.ServiceProvider, None);
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            var homeId = await db.Pages.Where(p => p.Slug == "/").Select(p => p.Id).SingleAsync(None);
            var homeSections = await db.PageSections.Where(s => s.PageId == homeId).ToListAsync(None);

            // Every keyed slot exists exactly once; the hero keeps the admin's content, order and visibility.
            Assert.Equal(homeSections.Count, homeSections.Select(s => s.SectionKey).Distinct().Count());
            Assert.Contains(homeSections, s => s.SectionKey == "story");
            var hero = Assert.Single(homeSections, s => s.SectionKey == "hero");
            Assert.Equal(("Admin đã sửa", 99, false), (hero.Heading, hero.DisplayOrder, hero.IsVisible));
            Assert.Single(await db.Pages.Where(p => p.Slug == "/").ToListAsync(None));

            // Pages the website renders but the database lacked are created with their key, and filled.
            var about = await db.Pages.SingleAsync(p => p.PageKey == "gioi-thieu", None);
            Assert.Equal(("/gioi-thieu", PublishStatus.Published), (about.Slug, about.Status));
            var aboutKeys = await db.PageSections.Where(s => s.PageId == about.Id).OrderBy(s => s.DisplayOrder)
                .Select(s => s.SectionKey).ToListAsync(None);
            Assert.Equal(["hero", "timeline", "signature-dish", "michelin"], aboutKeys);
            Assert.True(await db.Pages.AnyAsync(p => p.PageKey == "tin-tuc", None));
        }
    }
}

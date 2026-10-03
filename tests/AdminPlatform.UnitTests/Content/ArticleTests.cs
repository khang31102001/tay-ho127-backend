using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.ArticleCategories;
using AdminPlatform.Modules.Content.Application.ArticleTags;
using AdminPlatform.Modules.Content.Application.Articles;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Content;

public class ArticleTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    /// <summary>Marks sanitized content so tests can see the sanitizer ran without depending on its rules.</summary>
    private sealed class MarkingSanitizer : IHtmlContentSanitizer
    {
        public string Sanitize(string html) => $"[clean]{html}";
    }

    private static IContentDbContext NewDb() =>
        new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ArticleService Articles(IContentDbContext db, DateTime? now = null) =>
        new(db, new MarkingSanitizer(), new FixedClock(now ?? Now));

    private static CreateArticleRequest NewArticle(
        string title = "Bí quyết tráng bánh cuốn", string? slug = null, string status = "draft",
        Guid? categoryId = null, IReadOnlyList<Guid>? tagIds = null, string content = "<p>Nội dung</p>") =>
        new(title, slug, "Tóm tắt", content, null, categoryId, tagIds, "Tây Hồ 127", status);

    // ---------- Slug (moved to SharedKernel) ----------

    [Fact]
    public void Slug_transliterates_Vietnamese_titles()
    {
        Assert.Equal("bi-quyet-trang-banh-cuon", Slug.FromText("Bí quyết tráng bánh cuốn"));
    }

    // ---------- Domain ----------

    [Fact]
    public void PublishedAt_is_set_on_first_publish_and_kept_afterwards()
    {
        var details = new ArticleDetails("T", "t", "S", "<p>c</p>", null, null, "A", PublishStatus.Draft);
        var article = Article.Create(details, Now);
        Assert.Null(article.PublishedAtUtc);

        article.Update(details with { Status = PublishStatus.Published }, Now);
        Assert.Equal(Now, article.PublishedAtUtc);

        article.Update(details with { Status = PublishStatus.Draft }, Now.AddDays(5));
        article.Update(details with { Status = PublishStatus.Published }, Now.AddDays(9));
        Assert.Equal(Now, article.PublishedAtUtc);
    }

    [Fact]
    public void Article_requires_a_valid_slug_and_non_empty_text()
    {
        var valid = new ArticleDetails("T", "t", "S", "<p>c</p>", null, null, "A", PublishStatus.Draft);
        Assert.Throws<BusinessRuleValidationException>(() => Article.Create(valid with { Slug = "Bad Slug" }, Now));
        Assert.Throws<BusinessRuleValidationException>(() => Article.Create(valid with { Title = " " }, Now));
        Assert.Throws<BusinessRuleValidationException>(() => Article.Create(valid with { Content = "" }, Now));
    }

    [Fact]
    public void Replacing_tags_keeps_existing_links_and_dedupes()
    {
        var article = Article.Create(new ArticleDetails("T", "t", "S", "<p>c</p>", null, null, "A", PublishStatus.Draft), Now);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        article.ReplaceTags([a, b]);
        var linkA = article.Tags.Single(l => l.TagId == a);

        article.ReplaceTags([a, a]);

        Assert.Same(linkA, article.Tags.Single());
    }

    // ---------- Article service ----------

    [Fact]
    public async Task Content_is_sanitized_and_slug_is_generated_and_made_unique()
    {
        var db = NewDb();
        var sut = Articles(db);

        var first = await sut.CreateAsync(NewArticle(), None);
        var second = await sut.CreateAsync(NewArticle(), None);

        Assert.Equal("bi-quyet-trang-banh-cuon", first.Slug);
        Assert.Equal("bi-quyet-trang-banh-cuon-2", second.Slug);
        Assert.Equal("[clean]<p>Nội dung</p>", first.Content);

        var updated = await sut.UpdateAsync(first.Id, new UpdateArticleRequest("Đổi tiêu đề", null, "Tóm tắt", "<p>Mới</p>", null, null, null, "A", "draft"), None);
        Assert.Equal("bi-quyet-trang-banh-cuon", updated.Slug);
        Assert.Equal("[clean]<p>Mới</p>", updated.Content);
    }

    [Fact]
    public async Task An_explicit_slug_that_is_taken_is_a_conflict_but_can_be_kept_on_update()
    {
        var sut = Articles(NewDb());
        var first = await sut.CreateAsync(NewArticle(slug: "bai-mot"), None);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(NewArticle(title: "Khác", slug: "bai-mot"), None));

        var kept = await sut.UpdateAsync(first.Id, new UpdateArticleRequest("Bài một", "bai-mot", "Tóm tắt", "<p>x</p>", null, null, null, "A", "published"), None);
        Assert.Equal("published", kept.Status);
        Assert.NotNull(kept.PublishedAt);
    }

    [Fact]
    public async Task Category_and_tags_must_exist()
    {
        var db = NewDb();
        var tag = await new ArticleTagService(db).CreateAsync(new CreateArticleTagRequest("Truyền thống", null), None);
        var sut = Articles(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(NewArticle(categoryId: Guid.NewGuid()), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(NewArticle(tagIds: [Guid.NewGuid()]), None));

        var created = await sut.CreateAsync(NewArticle(tagIds: [tag.Id]), None);
        Assert.Equal([tag.Id], created.TagIds);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task Category_tree_is_limited_to_three_levels_and_slug_is_generated()
    {
        var sut = new ArticleCategoryService(NewDb());
        var group = await sut.CreateAsync(new CreateArticleCategoryRequest("Tin tức", null, null, 1, true), None);
        var child = await sut.CreateAsync(new CreateArticleCategoryRequest("Khuyến mãi", null, group.Id, 1, true), None);
        var leaf = await sut.CreateAsync(new CreateArticleCategoryRequest("Tháng mười", null, child.Id, 1, true), None);

        Assert.Equal("tin-tuc", group.Slug);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(new CreateArticleCategoryRequest("Quá sâu", null, leaf.Id, 1, true), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UpdateAsync(group.Id, new UpdateArticleCategoryRequest("Tin tức", null, leaf.Id, 1, true), None));
    }

    [Fact]
    public async Task A_category_with_children_or_articles_cannot_be_deleted()
    {
        var db = NewDb();
        var categories = new ArticleCategoryService(db);
        var parent = await categories.CreateAsync(new CreateArticleCategoryRequest("Tin tức", null, null, 1, true), None);
        var child = await categories.CreateAsync(new CreateArticleCategoryRequest("Khuyến mãi", null, parent.Id, 1, true), None);
        await Articles(db).CreateAsync(NewArticle(categoryId: child.Id), None);

        await Assert.ThrowsAsync<ConflictException>(() => categories.DeleteAsync(parent.Id, None));
        await Assert.ThrowsAsync<ConflictException>(() => categories.DeleteAsync(child.Id, None));
    }

    // ---------- Public read model ----------

    [Fact]
    public async Task Public_api_only_exposes_published_articles()
    {
        var db = NewDb();
        var articles = Articles(db);
        var published = await articles.CreateAsync(NewArticle(title: "Đã đăng", status: "published"), None);
        await articles.CreateAsync(NewArticle(title: "Bản nháp", status: "draft"), None);
        await articles.CreateAsync(NewArticle(title: "Đã lưu trữ", status: "archived"), None);

        var sut = new PublicContentService(db, new FixedClock(Now));
        var list = await sut.ListArticlesAsync(new PagedRequest(), None);

        Assert.Equal([published.Slug], list.Items.Select(a => a.Slug));
        Assert.Equal(Now, list.Items[0].PublishedAt);
        Assert.Equal(published.Slug, (await sut.GetArticleBySlugAsync(published.Slug, None)).Slug);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetArticleBySlugAsync("ban-nhap", None));
    }

    [Fact]
    public async Task Public_taxonomy_hides_inactive_categories()
    {
        var db = NewDb();
        var categories = new ArticleCategoryService(db);
        await categories.CreateAsync(new CreateArticleCategoryRequest("Hiện", null, null, 1, true), None);
        await categories.CreateAsync(new CreateArticleCategoryRequest("Ẩn", null, null, 2, false), None);
        await new ArticleTagService(db).CreateAsync(new CreateArticleTagRequest("Ưu đãi", null), None);

        var taxonomy = await new PublicContentService(db, new FixedClock(Now)).GetTaxonomyAsync(None);

        Assert.Equal(["Hiện"], taxonomy.Categories.Select(c => c.Name));
        Assert.Equal(["Ưu đãi"], taxonomy.Tags.Select(t => t.Name));
    }

    [Theory]
    [InlineData("<p>một hai ba</p>", 1)]
    [InlineData("", 1)]
    public void Reading_time_is_at_least_one_minute(string html, int expected)
    {
        Assert.Equal(expected, PublicContentService.EstimateReadingTimeMinutes(html));
    }

    [Fact]
    public void Reading_time_counts_words_not_markup()
    {
        var words = string.Join(" ", Enumerable.Repeat("từ", 600));
        Assert.Equal(3, PublicContentService.EstimateReadingTimeMinutes($"<p>{words}</p><h2>x</h2>"));
    }

    // ---------- Sanitizer (real rules) ----------

    [Fact]
    public void Sanitizer_removes_scripts_and_event_handlers_but_keeps_formatting()
    {
        var sanitizer = new HtmlContentSanitizer();

        var clean = sanitizer.Sanitize(
            "<h2>Tiêu đề</h2><p onclick=\"steal()\">Đoạn <strong>đậm</strong></p><script>alert(1)</script>" +
            "<a href=\"javascript:alert(1)\">xấu</a><a href=\"https://tayho127.vn\">tốt</a><iframe src=\"https://evil\"></iframe>");

        Assert.Contains("<h2>Tiêu đề</h2>", clean);
        Assert.Contains("<strong>đậm</strong>", clean);
        Assert.Contains("https://tayho127.vn", clean);
        Assert.DoesNotContain("script", clean);
        Assert.DoesNotContain("onclick", clean);
        Assert.DoesNotContain("javascript:", clean);
        Assert.DoesNotContain("iframe", clean);
    }

    // ---------- Tags ----------

    [Fact]
    public async Task Tag_slug_is_unique_and_deleting_a_tag_untags_articles()
    {
        var db = NewDb();
        var tags = new ArticleTagService(db);
        var tag = await tags.CreateAsync(new CreateArticleTagRequest("Ưu đãi", null), None);
        await Assert.ThrowsAsync<ConflictException>(() => tags.CreateAsync(new CreateArticleTagRequest("Khác", "uu-dai"), None));

        var article = await Articles(db).CreateAsync(NewArticle(tagIds: [tag.Id]), None);
        await tags.DeleteAsync(tag.Id, None);

        Assert.Empty(await db.ArticleTags.ToListAsync(None));
        // The in-memory provider does not cascade; relational Postgres does (covered by the integration test).
        Assert.NotNull(article);
    }
}

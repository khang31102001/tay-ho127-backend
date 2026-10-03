using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application.ArticleCategories;
using AdminPlatform.Modules.Content.Application.ArticleTags;
using AdminPlatform.Modules.Content.Application.Articles;
using AdminPlatform.Modules.Content.Application.PublicContent;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class ContentArticlesTests
{
    private readonly AdminPlatformApiFactory _factory;

    public ContentArticlesTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Articles_are_managed_by_an_admin_and_only_published_ones_reach_the_public_api()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        var category = await CreateAsync<ArticleCategoryResponse>(admin, "/api/v1/content/article-categories",
            new CreateArticleCategoryRequest($"Công thức {suffix}", null, null, 1, true));
        var tag = await CreateAsync<ArticleTagResponse>(admin, "/api/v1/content/article-tags",
            new CreateArticleTagRequest($"Truyền thống {suffix}", null));

        var published = await CreateAsync<ArticleResponse>(admin, "/api/v1/content/articles", new CreateArticleRequest(
            $"Bí quyết {suffix}", null, "Tóm tắt", "<p onclick=\"x()\">Nội dung</p><script>alert(1)</script>", null,
            category.Id, [tag.Id], "Tây Hồ 127", "published"));
        var draft = await CreateAsync<ArticleResponse>(admin, "/api/v1/content/articles", new CreateArticleRequest(
            $"Nháp {suffix}", null, "Tóm tắt", "<p>Nháp</p>", null, category.Id, null, "Tây Hồ 127", "draft"));

        // HTML is sanitized on save; publishing stamps the date; the slug is generated.
        Assert.DoesNotContain("script", published.Content);
        Assert.DoesNotContain("onclick", published.Content);
        Assert.NotNull(published.PublishedAt);
        Assert.Equal($"bi-quyet-{suffix}", published.Slug);
        Assert.Equal([tag.Id], published.TagIds);

        // Duplicate explicit slug is a conflict; a category that still has articles cannot be deleted.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/content/articles", new CreateArticleRequest(
            "Trùng", published.Slug, "Tóm tắt", "<p>x</p>", null, null, null, "A", "draft"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/content/article-categories/{category.Id}")).StatusCode);

        // The admin list omits the body but includes both articles.
        var list = (await admin.GetFromJsonAsync<PagedResult<ArticleListItemResponse>>($"/api/v1/content/articles?categoryId={category.Id}"))!;
        Assert.Equal(2, list.Items.Count);

        // Anonymous visitors only see the published article.
        using var anonymous = _factory.CreateClient();
        var publicList = (await anonymous.GetFromJsonAsync<PagedResult<PublicArticleSummaryResponse>>("/api/v1/content/public/articles?pageSize=200"))!;
        Assert.Contains(publicList.Items, a => a.Slug == published.Slug);
        Assert.DoesNotContain(publicList.Items, a => a.Slug == draft.Slug);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/v1/content/public/articles/{published.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/content/public/articles/{draft.Slug}")).StatusCode);
        var taxonomy = (await anonymous.GetFromJsonAsync<PublicTaxonomyResponse>("/api/v1/content/public/taxonomy"))!;
        Assert.Contains(taxonomy.Categories, c => c.Id == category.Id);

        // Managing content is not anonymous.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/content/articles")).StatusCode);

        // Deleting a tag only untags (cascade); deleting the articles frees the category.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/article-tags/{tag.Id}")).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<ArticleResponse>($"/api/v1/content/articles/{published.Id}"))!.TagIds);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/articles/{published.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/articles/{draft.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/article-categories/{category.Id}")).StatusCode);
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application.PageSections;
using AdminPlatform.Modules.Content.Application.Pages;
using AdminPlatform.Modules.Content.Application.PublicContent;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class ContentPagesTests
{
    private readonly AdminPlatformApiFactory _factory;

    public ContentPagesTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Pages_and_sections_are_managed_by_an_admin_and_published_pages_are_listed_publicly()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        var published = await CreateAsync<PageResponse>(admin, "/api/v1/content/pages", new CreatePageRequest($"Trang test {suffix}", $"/test-{suffix}", "published"));
        var draft = await CreateAsync<PageResponse>(admin, "/api/v1/content/pages", new CreatePageRequest($"Nháp test {suffix}", null, "draft"));
        Assert.NotNull(published.PublishedAt);
        Assert.StartsWith("/nhap-test", draft.Slug);

        // Duplicate path is a conflict; an invalid path is a 400.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/content/pages", new CreatePageRequest("Trùng", published.Slug, "draft"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/content/pages", new CreatePageRequest("Sai", "khong-co-gach", "draft"))).StatusCode);

        // Sections: create, order, update, cross-page 404, unsafe CTA.
        var sectionsUrl = $"/api/v1/content/pages/{published.Id}/sections";
        var second = await CreateAsync<PageSectionResponse>(admin, sectionsUrl, new CreatePageSectionRequest("cta", null, "Kêu gọi", null, "Nội dung", null, "Xem", "/thuc-don", 2, true));
        var first = await CreateAsync<PageSectionResponse>(admin, sectionsUrl, new CreatePageSectionRequest("hero", "Bánh cuốn", "TÂY HỒ", null, null, "media-a", null, null, 1, true));
        var list = (await admin.GetFromJsonAsync<List<PageSectionResponse>>(sectionsUrl))!;
        Assert.Equal([first.Id, second.Id], list.Select(s => s.Id));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(sectionsUrl, new CreatePageSectionRequest(
            "hero", null, null, null, null, null, null, "javascript:alert(1)", 3, true))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/content/pages/{draft.Id}/sections/{first.Id}")).StatusCode);

        var update = await admin.PutAsJsonAsync($"{sectionsUrl}/{first.Id}", new UpdatePageSectionRequest("introduction", null, "Mới", null, null, null, null, null, 1, false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        // The public list shows the published page only; managing pages is not anonymous.
        using var anonymous = _factory.CreateClient();
        var publicPages = (await anonymous.GetFromJsonAsync<List<PublicPageResponse>>("/api/v1/content/public/pages"))!;
        Assert.Contains(publicPages, p => p.Id == published.Id && p.Slug == published.Slug);
        Assert.DoesNotContain(publicPages, p => p.Id == draft.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/content/pages")).StatusCode);

        // Deleting a page deletes its sections.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/pages/{published.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(sectionsUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/pages/{draft.Id}")).StatusCode);

        var remaining = (await admin.GetFromJsonAsync<PagedResult<PageResponse>>($"/api/v1/content/pages?search={suffix}"))!;
        Assert.Empty(remaining.Items);
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

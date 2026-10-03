using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Application.Redirects;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class SeoRedirectsTests
{
    private readonly AdminPlatformApiFactory _factory;

    public SeoRedirectsTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Redirects_are_managed_by_an_admin_and_only_active_ones_reach_the_public_site()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];
        var source = $"/cu-{suffix}";

        // Bad input is rejected: reserved area, query string in the source, unsafe destination, self-redirect.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest("/admin/x", "/moi", 301, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest("/a?x=1", "/moi", 301, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest(source, "javascript:alert(1)", 301, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest(source, source, 301, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest(source, "/moi", 307, true))).StatusCode);

        var created = await CreateAsync(admin, new CreateRedirectRequest($"cu-{suffix}/", $"/moi-{suffix}", 301, true));
        Assert.Equal(source, created.SourcePath);

        // The same source (written differently) conflicts; a redirect back to it is a loop.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest(source, "/khac", 302, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/seo/redirects", new CreateRedirectRequest($"/moi-{suffix}", source, 301, true))).StatusCode);

        var list = (await admin.GetFromJsonAsync<PagedResult<RedirectResponse>>($"/api/v1/seo/redirects?search={suffix}&pageSize=200"))!;
        Assert.Contains(list.Items, r => r.Id == created.Id);

        // Anonymous visitors (the website's middleware) see the active redirect with its status code.
        using var anonymous = _factory.CreateClient();
        var publicList = (await anonymous.GetFromJsonAsync<List<PublicRedirectResponse>>("/api/v1/seo/public/redirects"))!;
        Assert.Contains(publicList, r => r.SourcePath == source && r.DestinationUrl == $"/moi-{suffix}" && r.RedirectType == 301);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/seo/redirects")).StatusCode);

        // Switching it off removes it from the public list.
        var off = await admin.PutAsJsonAsync($"/api/v1/seo/redirects/{created.Id}", new UpdateRedirectRequest(source, $"/moi-{suffix}", 301, false));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var after = (await anonymous.GetFromJsonAsync<List<PublicRedirectResponse>>("/api/v1/seo/public/redirects"))!;
        Assert.DoesNotContain(after, r => r.SourcePath == source);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/seo/redirects/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/seo/redirects/{created.Id}")).StatusCode);
    }

    private static async Task<RedirectResponse> CreateAsync(HttpClient client, CreateRedirectRequest body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/seo/redirects", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RedirectResponse>())!;
    }
}

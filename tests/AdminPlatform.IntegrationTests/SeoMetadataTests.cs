using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Application.Metadata;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class SeoMetadataTests
{
    private readonly AdminPlatformApiFactory _factory;

    public SeoMetadataTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seo_metadata_is_upserted_by_an_admin_and_readable_by_the_public_site()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var entityId = $"p-{Guid.NewGuid():n}"[..12];
        var encoded = Uri.EscapeDataString(entityId);

        // Invalid input is rejected: unknown type, missing id, unsafe canonical URL.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/metadata", Body("nope", entityId))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/metadata", Body("product", null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync("/api/v1/seo/metadata", Body("product", entityId, canonical: "javascript:alert(1)"))).StatusCode);

        // Upsert twice: the second call replaces, it does not duplicate.
        var first = await admin.PutAsJsonAsync("/api/v1/seo/metadata", Body("product", entityId, title: "Một"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var second = await admin.PutAsJsonAsync("/api/v1/seo/metadata", Body("product", entityId, title: "Hai", index: false));
        var saved = (await second.Content.ReadFromJsonAsync<SeoMetadataResponse>())!;
        Assert.Equal("Hai", saved.MetaTitle);
        Assert.False(saved.RobotsIndex);

        var list = (await admin.GetFromJsonAsync<PagedResult<SeoMetadataResponse>>(
            $"/api/v1/seo/metadata?entityType=product&search={encoded}&pageSize=200"))!;
        Assert.Single(list.Items);

        // The public site reads the override and sees the entity flagged as noindex — without signing in.
        using var anonymous = _factory.CreateClient();
        var publicRow = (await anonymous.GetFromJsonAsync<SeoMetadataResponse>($"/api/v1/seo/public/metadata?entityType=product&entityId={encoded}"))!;
        Assert.Equal("Hai", publicRow.MetaTitle);
        var noIndex = (await anonymous.GetFromJsonAsync<List<NoIndexEntityResponse>>("/api/v1/seo/public/noindex"))!;
        Assert.Contains(noIndex, e => e.EntityType == "product" && e.EntityId == entityId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/seo/metadata")).StatusCode);

        // Reset removes it; resetting again is still a success, and the public lookup is 404 again.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/seo/metadata?entityType=product&entityId={encoded}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/seo/metadata?entityType=product&entityId={encoded}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/seo/public/metadata?entityType=product&entityId={encoded}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/seo/metadata/lookup?entityType=product&entityId={encoded}")).StatusCode);
    }

    private static UpsertSeoMetadataRequest Body(
        string type, string? id, string? title = "Tiêu đề", string? canonical = null, bool index = true) =>
        new(type, id, title, "Mô tả", canonical, index, true, null, null, null, null, null, null);
}

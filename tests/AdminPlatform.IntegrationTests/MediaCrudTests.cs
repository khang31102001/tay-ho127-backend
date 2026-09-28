using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Media.Application.Media;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class MediaCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public MediaCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Media_can_be_created_read_updated_and_soft_deleted()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var url = $"https://cdn.integration.test/{Guid.NewGuid():n}.png";

        var createResponse = await client.PostAsJsonAsync("/api/v1/media", new CreateMediaRequest("photo.png", url, "image", "alt", 1024));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var media = (await createResponse.Content.ReadFromJsonAsync<MediaResponse>())!;
        Assert.Equal("Image", media.Type);
        Assert.Equal("Active", media.Status);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/media/{media.Id}", new UpdateMediaRequest("renamed.pdf", url, "document", null));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Document", (await updateResponse.Content.ReadFromJsonAsync<MediaResponse>())!.Type);

        // DELETE is a soft delete: the entry stays readable (other modules may still reference its id)
        // but is marked Inactive.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/media/{media.Id}")).StatusCode);
        var deleted = await client.GetFromJsonAsync<MediaResponse>($"/api/v1/media/{media.Id}");
        Assert.Equal("Inactive", deleted!.Status);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/media/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Listing_can_be_filtered_by_type()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/media", new CreateMediaRequest("clip.mp4", $"https://cdn.integration.test/{Guid.NewGuid():n}.mp4", "video", null, 1));

        var page = await client.GetFromJsonAsync<PagedResultDto<MediaResponse>>("/api/v1/media?page=1&pageSize=100&kind=video");

        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, m => Assert.Equal("Video", m.Type));
    }

    [Fact]
    public async Task An_unknown_type_filter_returns_400_not_500()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/media?kind=bogus")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/media?status=bogus")).StatusCode);
    }

    [Fact]
    public async Task Invalid_create_request_returns_400()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/media", new CreateMediaRequest("", "", "exe", null, -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

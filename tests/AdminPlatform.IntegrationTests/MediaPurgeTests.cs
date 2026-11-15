using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Media.Application.Media;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class MediaPurgeTests
{
    private readonly AdminPlatformApiFactory _factory;

    public MediaPurgeTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Media_used_by_a_product_cannot_be_purged_until_it_is_unlinked_then_file_and_record_are_gone()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0]), "file", $"purge-{suffix}.png" },
        };
        var uploadResponse = await client.PostAsync("/api/v1/media/upload", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var media = (await uploadResponse.Content.ReadFromJsonAsync<MediaResponse>())!;
        var filePath = new Uri(media.Url).AbsolutePath;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(filePath)).StatusCode);

        var group = await PostAsync<CategoryResponse>(client, "/api/v1/catalog/categories", new CreateCategoryRequest($"Nhóm {suffix}", null, 1, true));
        var leaf = await PostAsync<CategoryResponse>(client, "/api/v1/catalog/categories", new CreateCategoryRequest($"Lá {suffix}", group.Id, 1, true));
        var product = await PostAsync<ProductResponse>(client, "/api/v1/catalog/products", new CreateProductRequest(
            $"Món {suffix}", null, leaf.Id, 10000, null, null, null, true, null, null, null, [media.Id.ToString()], [], media.Id));

        // Đang là ảnh đại diện + ảnh trong danh sách → bị chặn, file và bản ghi còn nguyên.
        var usages = await client.GetFromJsonAsync<List<MediaUsageResponse>>($"/api/v1/media/{media.Id}/usages");
        Assert.Contains(usages!, u => u.EntityId == product.Id.ToString() && u.Field == "Ảnh đại diện");
        Assert.Contains(usages!, u => u.EntityId == product.Id.ToString() && u.Field == "Danh sách ảnh");

        var blocked = await PurgeAsync(client, media.Id);
        Assert.Equal(0, blocked.DeletedCount);
        Assert.Equal(PurgeMediaStatus.InUse, Assert.Single(blocked.Results).Status);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/media/{media.Id}")).StatusCode);

        // Gỡ khỏi sản phẩm rồi xóa vĩnh viễn: bản ghi và file đều mất.
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}", new UpdateProductRequest(
            product.Name, null, leaf.Id, 10000, null, null, null, true, null, null, null, [], [], null))).StatusCode);

        var purged = await PurgeAsync(client, media.Id);
        Assert.Equal(1, purged.DeletedCount);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/media/{media.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(filePath)).StatusCode);

        await client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
        await client.DeleteAsync($"/api/v1/catalog/categories/{leaf.Id}");
        await client.DeleteAsync($"/api/v1/catalog/categories/{group.Id}");
    }

    [Fact]
    public async Task Purge_validates_the_batch_and_requires_authentication()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/media/purge", new PurgeMediaRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/media/purge",
            new PurgeMediaRequest(Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList()))).StatusCode);

        var unknown = await PurgeAsync(client, Guid.NewGuid());
        Assert.Equal(PurgeMediaStatus.NotFound, Assert.Single(unknown.Results).Status);

        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/media/purge", new PurgeMediaRequest([Guid.NewGuid()]))).StatusCode);
    }

    private static async Task<PurgeMediaResponse> PurgeAsync(HttpClient client, Guid id)
    {
        var response = await client.PostAsJsonAsync("/api/v1/media/purge", new PurgeMediaRequest([id]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurgeMediaResponse>())!;
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

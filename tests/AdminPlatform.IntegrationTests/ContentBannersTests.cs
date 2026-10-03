using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application.Banners;
using AdminPlatform.Modules.Content.Application.PublicContent;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class ContentBannersTests
{
    private readonly AdminPlatformApiFactory _factory;

    public ContentBannersTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Banners_are_managed_by_an_admin_and_only_live_ones_reach_the_public_api()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        var live = await CreateAsync(admin, new CreateBannerRequest($"Live {suffix}", "media-a", null, "Alt", "Tiêu đề", null, "Xem", "/thuc-don",
            "HOME_HERO", null, null, 1, true));
        var off = await CreateAsync(admin, new CreateBannerRequest($"Off {suffix}", null, null, "Alt", null, null, null, null,
            "HOME_HERO", null, null, 2, false));
        var expired = await CreateAsync(admin, new CreateBannerRequest($"Expired {suffix}", null, null, "Alt", null, null, null, null,
            "HOME_HERO", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1), 3, true));

        // Unsafe CTA URLs and unknown placements are rejected.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/content/banners", new CreateBannerRequest(
            "x", null, null, "Alt", null, null, null, "javascript:alert(1)", "HOME_HERO", null, null, 1, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/content/banners", new CreateBannerRequest(
            "x", null, null, "Alt", null, null, null, null, "NOWHERE", null, null, 1, true))).StatusCode);

        var list = (await admin.GetFromJsonAsync<PagedResult<BannerResponse>>("/api/v1/content/banners?placement=HOME_HERO&pageSize=200"))!;
        Assert.Contains(list.Items, b => b.Id == live.Id);

        // Anonymous visitors see only the live banner.
        using var anonymous = _factory.CreateClient();
        var publicBanners = (await anonymous.GetFromJsonAsync<List<PublicBannerResponse>>("/api/v1/content/public/banners?placement=HOME_HERO"))!;
        Assert.Contains(publicBanners, b => b.Id == live.Id);
        Assert.DoesNotContain(publicBanners, b => b.Id == off.Id || b.Id == expired.Id);

        // Switching the banner off removes it from the public list; managing banners is not anonymous.
        var update = await admin.PutAsJsonAsync($"/api/v1/content/banners/{live.Id}", new UpdateBannerRequest(
            live.Name, null, null, "Alt", null, null, null, null, "HOME_HERO", null, null, 1, false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var after = (await anonymous.GetFromJsonAsync<List<PublicBannerResponse>>("/api/v1/content/public/banners?placement=HOME_HERO"))!;
        Assert.DoesNotContain(after, b => b.Id == live.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/content/banners")).StatusCode);

        foreach (var banner in new[] { live, off, expired })
        {
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/content/banners/{banner.Id}")).StatusCode);
        }
    }

    private static async Task<BannerResponse> CreateAsync(HttpClient client, CreateBannerRequest body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/content/banners", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BannerResponse>())!;
    }
}

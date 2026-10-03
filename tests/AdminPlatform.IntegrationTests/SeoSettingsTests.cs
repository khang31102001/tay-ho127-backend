using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Seo.Application.Settings;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class SeoSettingsTests
{
    private readonly AdminPlatformApiFactory _factory;

    public SeoSettingsTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seo_settings_are_managed_by_an_admin_and_readable_by_the_public_site()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        // Invalid input is rejected: no "%s" placeholder, and a robots path that is not a site path.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/settings", Body("Tây Hồ", $"Mô tả {suffix}", ["/admin"]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/settings", Body("%s | Tây Hồ", $"Mô tả {suffix}", ["//evil.com"]))).StatusCode);

        var update = await admin.PutAsJsonAsync("/api/v1/seo/settings", Body("%s | Tây Hồ", $"Mô tả {suffix}", [" /admin ", "/gio-hang", "/admin"]));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var saved = (await update.Content.ReadFromJsonAsync<SeoSettingsResponse>())!;
        Assert.Equal(["/admin", "/gio-hang"], saved.RobotsDisallowPaths);

        var read = (await admin.GetFromJsonAsync<SeoSettingsResponse>("/api/v1/seo/settings"))!;
        Assert.Equal($"Mô tả {suffix}", read.DefaultDescription);

        // The public website reads it without signing in, but cannot change it.
        using var anonymous = _factory.CreateClient();
        var publicSettings = (await anonymous.GetFromJsonAsync<SeoSettingsResponse>("/api/v1/seo/public/settings"))!;
        Assert.Equal($"Mô tả {suffix}", publicSettings.DefaultDescription);
        Assert.Equal(["/admin", "/gio-hang"], publicSettings.RobotsDisallowPaths);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/seo/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PutAsJsonAsync("/api/v1/seo/settings", Body("%s", "x", []))).StatusCode);
    }

    private static UpdateSeoSettingsRequest Body(string template, string description, string[] paths) =>
        new(template, description, null, "@tayho127", null, true, true, paths);
}

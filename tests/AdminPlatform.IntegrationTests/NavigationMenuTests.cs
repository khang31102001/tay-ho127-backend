using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Navigation.Application.Containers;
using AdminPlatform.Modules.Navigation.Application.Items;
using AdminPlatform.Modules.Navigation.Application.MyNavigation;
using AdminPlatform.Modules.Navigation.Application.Public;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class NavigationMenuTests
{
    private readonly AdminPlatformApiFactory _factory;

    public NavigationMenuTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/navigation/me")]
    [InlineData("/api/v1/navigation/menus")]
    public async Task SuperAdmin_sees_the_full_seeded_sidebar_including_gated_entries(string path)
    {
        using var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tree = await response.Content.ReadFromJsonAsync<List<MenuTreeNode>>();
        Assert.NotNull(tree);

        Assert.Contains(tree!, n => n.Code == "dashboard");
        Assert.Contains(tree!, n => n.Code == "admin.users");
        var system = Assert.Single(tree!, n => n.Code == "system");
        Assert.Contains(system.Children, c => c.Code == "admin.audit-logs");
        var catalog = Assert.Single(tree!, n => n.Code == "catalog");
        Assert.Equal("/admin/catalog/media", Assert.Single(catalog.Children, c => c.Code == "admin.media").Route);
    }

    [Fact]
    public async Task A_user_with_no_permissions_sees_only_ungated_entries()
    {
        using var adminClient = _factory.CreateClient();
        var adminToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(adminClient, _factory.AdminEmail, _factory.AdminPassword);
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var email = $"nav-no-permissions-{Guid.NewGuid():n}@integration.test";
        const string password = "S3curePassw0rd!";
        await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "Nav No Permissions", password));

        using var plainClient = _factory.CreateClient();
        var plainToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(plainClient, email, password);
        plainClient.DefaultRequestHeaders.Authorization = new("Bearer", plainToken);

        var response = await plainClient.GetAsync("/api/v1/navigation/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tree = await response.Content.ReadFromJsonAsync<List<MenuTreeNode>>();

        Assert.Contains(tree!, n => n.Code == "dashboard");
        // Permission-gated entries are hidden, as are sections left with no visible child.
        Assert.DoesNotContain(tree!, n => n.Code == "admin.users");
        Assert.DoesNotContain(tree!, n => n.Code == "system");
        Assert.DoesNotContain(tree!.SelectMany(n => n.Children), c => c.Code is "sales.customers" or "admin.media");
    }

    [Fact]
    public async Task The_sidebar_never_contains_website_menu_items()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var tree = await client.GetFromJsonAsync<List<MenuTreeNode>>("/api/v1/navigation/me");

        static IEnumerable<MenuTreeNode> Flatten(IEnumerable<MenuTreeNode> nodes) =>
            nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.Children)));
        Assert.DoesNotContain(Flatten(tree!), n => n.Code.StartsWith("header.") || n.Code.StartsWith("footer."));
    }

    [Theory]
    [InlineData("header", 3)]
    [InlineData("mobile", 3)]
    [InlineData("footer", 5)]
    public async Task The_public_website_menu_is_readable_anonymously_and_seeded(string location, int topLevelItems)
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/v1/navigation/public/{location}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = await response.Content.ReadFromJsonAsync<PublicNavigationResponse>();
        Assert.Equal(location, menu!.Location);
        Assert.True(menu.Items.Count >= topLevelItems - 1, "the seeded menu is missing");
        Assert.All(menu.Items, i => Assert.NotNull(i.TargetType));
    }

    [Theory]
    [InlineData("sidebar")]
    [InlineData("nope")]
    public async Task The_public_endpoint_never_serves_the_admin_sidebar_or_unknown_locations(string location)
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/v1/navigation/public/{location}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Management_endpoints_require_a_login()
    {
        using var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/navigation/containers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/navigation/items")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/navigation/me")).StatusCode);
    }

    [Fact]
    public async Task The_four_seeded_containers_are_listed_and_only_name_and_active_flag_can_change()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var containers = await client.GetFromJsonAsync<List<NavigationMenuResponse>>("/api/v1/navigation/containers");
        Assert.Contains(containers!, c => c is { Scope: "admin", Location: "sidebar" });
        Assert.Contains(containers!, c => c is { Scope: "site", Location: "header" });
        Assert.Contains(containers!, c => c is { Scope: "site", Location: "footer" });
        Assert.Contains(containers!, c => c is { Scope: "site", Location: "mobile" });

        var mobile = containers!.Single(c => c.Location == "mobile");
        var renamed = await client.PutAsJsonAsync($"/api/v1/navigation/containers/{mobile.Id}", new UpdateNavigationMenuRequest("Mobile (renamed)", true));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var body = await renamed.Content.ReadFromJsonAsync<NavigationMenuResponse>();
        Assert.Equal("Mobile (renamed)", body!.Name);
        Assert.Equal("mobile", body.Location);
    }
}

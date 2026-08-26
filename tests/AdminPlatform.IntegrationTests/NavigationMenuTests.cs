using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Navigation.Application.Menus;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class NavigationMenuTests
{
    private readonly AdminPlatformApiFactory _factory;

    public NavigationMenuTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuperAdmin_sees_the_full_seeded_menu_tree_including_gated_entries()
    {
        using var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/api/v1/navigation/menus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tree = await response.Content.ReadFromJsonAsync<List<MenuTreeNode>>();
        Assert.NotNull(tree);

        Assert.Contains(tree!, n => n.Code == "dashboard");
        var admin = Assert.Single(tree!, n => n.Code == "admin");
        Assert.Contains(admin.Children, c => c.Code == "admin.users");
    }

    [Fact]
    public async Task A_user_with_no_permissions_only_sees_the_public_dashboard_entry()
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

        var response = await plainClient.GetAsync("/api/v1/navigation/menus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tree = await response.Content.ReadFromJsonAsync<List<MenuTreeNode>>();

        Assert.Contains(tree!, n => n.Code == "dashboard");
        Assert.DoesNotContain(tree!, n => n.Code == "admin");
    }
}

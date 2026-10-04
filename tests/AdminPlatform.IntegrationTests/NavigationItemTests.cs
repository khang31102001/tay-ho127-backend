using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using AdminPlatform.Modules.AccessControl.Application.UserRoles;
using AdminPlatform.Modules.Navigation.Application.Containers;
using AdminPlatform.Modules.Navigation.Application.Items;
using AdminPlatform.Modules.Navigation.Application.Public;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class NavigationItemTests
{
    private readonly AdminPlatformApiFactory _factory;

    public NavigationItemTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_website_item_can_be_created_edited_gated_blocked_and_deleted_and_shows_on_the_public_menu()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var header = await GetContainerAsync(client, "site", "header");
        var code = AuthTestHelper.UniqueCode("it");

        var created = await client.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(header.Id, code, "Khuyến mãi", null, false, "/khuyen-mai", null, 99,
                new NavigationSiteDetailRequest("route", null, false)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<NavigationItemResponse>())!;

        var duplicate = await client.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(header.Id, code, "Dup", null, false, "/x", null, 1, new("route", null, false)));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var anonymous = _factory.CreateClient();
        var publicMenu = await anonymous.GetFromJsonAsync<PublicNavigationResponse>("/api/v1/navigation/public/header");
        Assert.Contains(publicMenu!.Items, i => i.Id == item.Id);

        var updated = await client.PutAsJsonAsync($"/api/v1/navigation/items/{item.Id}",
            new UpdateNavigationItemRequest("Ưu đãi", true, null, false, "/uu-dai", null, 99, new("external", null, true)));
        Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode); // external needs an http(s) URL

        var updatedOk = await client.PutAsJsonAsync($"/api/v1/navigation/items/{item.Id}",
            new UpdateNavigationItemRequest("Ưu đãi", true, null, false, "https://example.com/uu-dai", null, 99, new("external", null, true)));
        Assert.Equal(HttpStatusCode.OK, updatedOk.StatusCode);

        // Website items cannot be gated yet.
        var gate = await client.PutAsJsonAsync($"/api/v1/navigation/items/{item.Id}/permissions", new AssignNavigationItemPermissionsRequest(["media.view"]));
        Assert.Equal(HttpStatusCode.BadRequest, gate.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/navigation/items/{item.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/navigation/items/{item.Id}")).StatusCode);
    }

    [Fact]
    public async Task An_admin_item_can_be_gated_by_a_permission_but_not_by_a_permission_group()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var sidebar = await GetContainerAsync(client, "admin", "sidebar");
        var created = await client.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(sidebar.Id, AuthTestHelper.UniqueCode("ad"), "Test link", null, false, "/admin/test", "Star", 99, null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<NavigationItemResponse>())!;

        var gate = await client.PutAsJsonAsync($"/api/v1/navigation/items/{item.Id}/permissions", new AssignNavigationItemPermissionsRequest(["media.view"]));
        Assert.Equal(HttpStatusCode.NoContent, gate.StatusCode);
        Assert.Equal(["media.view"], await client.GetFromJsonAsync<List<string>>($"/api/v1/navigation/items/{item.Id}/permissions"));

        var group = await client.PutAsJsonAsync($"/api/v1/navigation/items/{item.Id}/permissions", new AssignNavigationItemPermissionsRequest(["group:catalog"]));
        Assert.Equal(HttpStatusCode.BadRequest, group.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/navigation/items/{item.Id}")).StatusCode);
    }

    [Fact]
    public async Task Reorder_moves_items_and_the_depth_limit_is_enforced()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var mobile = await GetContainerAsync(client, "site", "mobile");
        var a = await CreateSiteItemAsync(client, mobile.Id, null);
        var b = await CreateSiteItemAsync(client, mobile.Id, a.Id);
        var c = await CreateSiteItemAsync(client, mobile.Id, b.Id);

        var tooDeep = await client.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(mobile.Id, null, "L4", c.Id, false, "/x", null, 1, new("route", null, false)));
        Assert.Equal(HttpStatusCode.BadRequest, tooDeep.StatusCode);

        var reorder = await client.PutAsJsonAsync("/api/v1/navigation/items/reorder", new ReorderNavigationItemsRequest(mobile.Id, null, [c.Id, b.Id]));
        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);
        var moved = await client.GetFromJsonAsync<NavigationItemResponse>($"/api/v1/navigation/items/{c.Id}");
        Assert.Null(moved!.ParentId);
        Assert.Equal(1, moved.SortOrder);

        foreach (var id in new[] { b.Id, c.Id, a.Id })
        {
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/navigation/items/{id}")).StatusCode);
        }
    }

    [Fact]
    public async Task A_content_editor_with_site_navigation_permissions_cannot_touch_the_admin_sidebar()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var codes = new[] { "site-navigation.view", "site-navigation.create", "site-navigation.update", "site-navigation.delete" };
        var all = await admin.GetFromJsonAsync<PagedResult<PermissionResponse>>("/api/v1/permissions?pageSize=200");
        var ids = all!.Items.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToList();
        Assert.Equal(codes.Length, ids.Count);

        var roleResponse = await admin.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(AuthTestHelper.UniqueCode("editor"), "Menu editor"));
        var role = (await roleResponse.Content.ReadFromJsonAsync<RoleResponse>())!;
        await admin.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest(ids));

        var email = $"menu-editor-{Guid.NewGuid():n}@integration.test";
        const string password = "S3curePassw0rd!";
        var userResponse = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "Menu Editor", password));
        var user = (await userResponse.Content.ReadFromJsonAsync<UserDetailsResponse>())!;
        await admin.PostAsJsonAsync($"/api/v1/users/{user.Id}/roles", new AssignRoleRequest(role.Id));

        using var editor = _factory.CreateClient();
        editor.DefaultRequestHeaders.Authorization = new("Bearer", await AuthTestHelper.LoginAndGetAccessTokenAsync(editor, email, password));

        var header = await GetContainerAsync(admin, "site", "header");
        var sidebar = await GetContainerAsync(admin, "admin", "sidebar");

        var siteOk = await editor.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(header.Id, AuthTestHelper.UniqueCode("ed"), "Editor link", null, false, "/x", null, 90, new("route", null, false)));
        Assert.Equal(HttpStatusCode.Created, siteOk.StatusCode);
        var adminDenied = await editor.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(sidebar.Id, AuthTestHelper.UniqueCode("ed"), "Hack", null, false, "/admin/hack", null, 90, null));
        Assert.Equal(HttpStatusCode.Forbidden, adminDenied.StatusCode);

        var containers = await editor.GetFromJsonAsync<List<NavigationMenuResponse>>("/api/v1/navigation/containers");
        Assert.All(containers!, c => Assert.Equal("site", c.Scope));

        var created = (await siteOk.Content.ReadFromJsonAsync<NavigationItemResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/api/v1/navigation/items/{created.Id}")).StatusCode);
    }

    private static async Task<NavigationMenuResponse> GetContainerAsync(HttpClient client, string scope, string location)
    {
        var containers = await client.GetFromJsonAsync<List<NavigationMenuResponse>>("/api/v1/navigation/containers");
        return containers!.Single(c => c.Scope == scope && c.Location == location);
    }

    private static async Task<NavigationItemResponse> CreateSiteItemAsync(HttpClient client, Guid menuId, Guid? parentId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/navigation/items",
            new CreateNavigationItemRequest(menuId, AuthTestHelper.UniqueCode("n"), "Test", parentId, false, "/thuc-don", null, 50, new("route", null, false)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NavigationItemResponse>())!;
    }
}

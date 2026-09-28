using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Navigation.Application.Menus;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class MenuCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public MenuCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Menu_can_be_created_read_updated_gated_and_deleted()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var menu = await CreateMenuAsync(client, parentId: null);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest(menu.Code, "Dup", null, null, null, 0));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/menus/{menu.Id}",
            new UpdateMenuRequest("Renamed Menu", true, null, "/renamed", "star", 5));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("/renamed", (await updateResponse.Content.ReadFromJsonAsync<MenuResponse>())!.Route);

        var gateResponse = await client.PutAsJsonAsync($"/api/v1/menus/{menu.Id}/permissions", new AssignMenuPermissionsRequest(["media.view"]));
        Assert.Equal(HttpStatusCode.NoContent, gateResponse.StatusCode);
        Assert.Equal(["media.view"], await client.GetFromJsonAsync<List<string>>($"/api/v1/menus/{menu.Id}/permissions"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/menus/{menu.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/menus/{menu.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_menu_with_children_cannot_be_deleted_and_cannot_be_its_own_parent()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var parent = await CreateMenuAsync(client, parentId: null);
        var child = await CreateMenuAsync(client, parent.Id);

        var selfParentResponse = await client.PutAsJsonAsync($"/api/v1/menus/{parent.Id}",
            new UpdateMenuRequest(parent.Name, true, parent.Id, parent.Route, parent.Icon, parent.SortOrder));
        Assert.Equal(HttpStatusCode.BadRequest, selfParentResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/menus/{parent.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/menus/{child.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/menus/{parent.Id}")).StatusCode);
    }

    [Fact]
    public async Task The_seeded_sidebar_includes_the_media_library_entry()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var tree = await client.GetFromJsonAsync<List<MenuTreeNode>>("/api/v1/navigation/menus");

        var admin = Assert.Single(tree!, n => n.Code == "admin");
        Assert.Contains(admin.Children, c => c.Code == "admin.media");
    }

    private static async Task<MenuResponse> CreateMenuAsync(HttpClient client, Guid? parentId)
    {
        var code = $"test.{Guid.NewGuid():n}";
        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest(code, "Test Menu", parentId, $"/{code}", "star", 1));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MenuResponse>())!;
    }
}

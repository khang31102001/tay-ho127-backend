using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using AdminPlatform.Modules.AccessControl.Application.Roles;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class PermissionHierarchyTests
{
    private readonly AdminPlatformApiFactory _factory;

    public PermissionHierarchyTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Selecting_a_group_grants_every_active_leaf_below_it_and_never_stores_the_group()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var module = await CreateGroupAsync(client, null);
        var resource = await CreateGroupAsync(client, module.Id);
        var viewLeaf = await CreateLeafAsync(client, resource.Id);
        var updateLeaf = await CreateLeafAsync(client, resource.Id);
        var role = await CreateRoleAsync(client);

        var response = await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([module.Id]));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var granted = await client.GetFromJsonAsync<List<Guid>>($"/api/v1/roles/{role.Id}/permissions");
        Assert.Equal(new[] { viewLeaf.Id, updateLeaf.Id }.ToHashSet(), granted!.ToHashSet());
        Assert.DoesNotContain(module.Id, granted!);
        Assert.DoesNotContain(resource.Id, granted!);
    }

    [Fact]
    public async Task A_leaf_added_to_a_group_later_is_not_granted_automatically()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var group = await CreateGroupAsync(client, null);
        var firstLeaf = await CreateLeafAsync(client, group.Id);
        var role = await CreateRoleAsync(client);
        await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([group.Id]));

        var laterLeaf = await CreateLeafAsync(client, group.Id);

        var granted = await client.GetFromJsonAsync<List<Guid>>($"/api/v1/roles/{role.Id}/permissions");
        Assert.Equal([firstLeaf.Id], granted);
        Assert.DoesNotContain(laterLeaf.Id, granted!);
    }

    [Fact]
    public async Task Tree_nests_groups_and_leaves_and_hides_inactive_nodes()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var module = await CreateGroupAsync(client, null);
        var resource = await CreateGroupAsync(client, module.Id);
        var leaf = await CreateLeafAsync(client, resource.Id);

        var tree = await client.GetFromJsonAsync<List<PermissionTreeNode>>("/api/v1/permissions/tree");
        var moduleNode = tree!.Single(n => n.Id == module.Id);
        var resourceNode = moduleNode.Children.Single();
        Assert.True(resourceNode.IsGroup);
        Assert.Equal(leaf.Id, resourceNode.Children.Single().Id);

        await client.PutAsJsonAsync($"/api/v1/permissions/{leaf.Id}", new UpdatePermissionRequest(leaf.Name, false, resource.Id, false, 0));
        var after = await client.GetFromJsonAsync<List<PermissionTreeNode>>("/api/v1/permissions/tree");
        Assert.Empty(after!.Single(n => n.Id == module.Id).Children.Single().Children);
    }

    [Fact]
    public async Task A_leaf_cannot_have_children_and_must_have_a_group_parent()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var group = await CreateGroupAsync(client, null);
        var leaf = await CreateLeafAsync(client, group.Id);

        var underLeaf = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"test.{Guid.NewGuid():n}", "Child of leaf", leaf.Id, false, 0));
        Assert.Equal(HttpStatusCode.BadRequest, underLeaf.StatusCode);

        var withoutParent = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"test.{Guid.NewGuid():n}", "Orphan", null, false, 0));
        Assert.Equal(HttpStatusCode.BadRequest, withoutParent.StatusCode);
    }

    [Fact]
    public async Task A_group_cannot_be_moved_under_its_own_descendant()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var top = await CreateGroupAsync(client, null);
        var child = await CreateGroupAsync(client, top.Id);

        var response = await client.PutAsJsonAsync($"/api/v1/permissions/{top.Id}",
            new UpdatePermissionRequest(top.Name, true, child.Id, true, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_tree_cannot_be_deeper_than_the_maximum()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var parentId = (Guid?)null;
        for (var level = 1; level <= 5; level++)
        {
            parentId = (await CreateGroupAsync(client, parentId)).Id;
        }

        var tooDeep = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"group:test-{Guid.NewGuid():n}", "Level 6", parentId, true, 0));

        Assert.Equal(HttpStatusCode.BadRequest, tooDeep.StatusCode);
    }

    [Fact]
    public async Task A_group_with_children_cannot_be_deleted_and_the_children_must_go_first()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var group = await CreateGroupAsync(client, null);
        var leaf = await CreateLeafAsync(client, group.Id);

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/permissions/{group.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/permissions/{leaf.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/permissions/{group.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_group_code_must_use_the_group_prefix_and_a_leaf_code_must_not()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var groupWithLeafCode = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest("resource.view", "Bad group", null, true, 0));
        Assert.Equal(HttpStatusCode.BadRequest, groupWithLeafCode.StatusCode);

        var group = await CreateGroupAsync(client, null);
        var leafWithGroupCode = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"group:x-{Guid.NewGuid():n}", "Bad leaf", group.Id, false, 0));
        Assert.Equal(HttpStatusCode.BadRequest, leafWithGroupCode.StatusCode);
    }

    [Fact]
    public async Task A_leaf_granted_to_a_role_cannot_become_a_group()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var group = await CreateGroupAsync(client, null);
        var leaf = await CreateLeafAsync(client, group.Id);
        var role = await CreateRoleAsync(client);
        await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([leaf.Id]));

        var response = await client.PutAsJsonAsync($"/api/v1/permissions/{leaf.Id}",
            new UpdatePermissionRequest(leaf.Name, true, group.Id, true, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    internal static async Task<PermissionResponse> CreateGroupAsync(HttpClient client, Guid? parentId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"group:test-{Guid.NewGuid():n}", "Test Group", parentId, true, 0));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PermissionResponse>())!;
    }

    internal static async Task<PermissionResponse> CreateLeafAsync(HttpClient client, Guid groupId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/permissions",
            new CreatePermissionRequest($"test.{Guid.NewGuid():n}", "Test Permission", groupId, false, 0));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PermissionResponse>())!;
    }

    private static async Task<RoleResponse> CreateRoleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(AuthTestHelper.UniqueCode("role"), "Test Role"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoleResponse>())!;
    }
}

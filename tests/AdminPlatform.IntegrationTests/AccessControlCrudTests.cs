using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using AdminPlatform.Modules.AccessControl.Application.UserRoles;
using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class AccessControlCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public AccessControlCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Role_can_be_created_read_updated_granted_permissions_and_deleted()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var code = AuthTestHelper.UniqueCode("role");

        var createResponse = await client.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(code, "Test Role"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var role = (await createResponse.Content.ReadFromJsonAsync<RoleResponse>())!;

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(code, "Duplicate"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}", new UpdateRoleRequest("Renamed Role", true));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Renamed Role", (await updateResponse.Content.ReadFromJsonAsync<RoleResponse>())!.Name);

        var permission = await CreatePermissionAsync(client);
        var grantResponse = await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([permission.Id]));
        Assert.Equal(HttpStatusCode.NoContent, grantResponse.StatusCode);
        var grantedIds = await client.GetFromJsonAsync<List<Guid>>($"/api/v1/roles/{role.Id}/permissions");
        Assert.Equal([permission.Id], grantedIds);

        var deleteResponse = await client.DeleteAsync($"/api/v1/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/roles/{role.Id}")).StatusCode);
    }

    [Fact]
    public async Task Granting_a_permission_id_that_does_not_exist_returns_400()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var role = await CreateRoleAsync(client);

        var response = await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_role_assigned_to_a_user_cannot_be_deleted_until_it_is_unassigned()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var role = await CreateRoleAsync(client);
        var user = await CreateUserAsync(client);

        var assignResponse = await client.PostAsJsonAsync($"/api/v1/users/{user.Id}/roles", new AssignRoleRequest(role.Id));
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);
        var userRoles = await client.GetFromJsonAsync<List<UserRoleResponse>>($"/api/v1/users/{user.Id}/roles");
        Assert.Contains(userRoles!, r => r.RoleId == role.Id);

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/roles/{role.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/users/{user.Id}/roles/{role.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/roles/{role.Id}")).StatusCode);
    }

    [Fact]
    public async Task Assigning_a_role_that_does_not_exist_returns_404()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var user = await CreateUserAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/users/{user.Id}/roles", new AssignRoleRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Permission_can_be_updated_and_cannot_be_deleted_while_granted_to_a_role()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var permission = await CreatePermissionAsync(client);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/permissions", new CreatePermissionRequest(permission.Code, "Duplicate", permission.ParentId, false, 0));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/permissions/{permission.Id}", new UpdatePermissionRequest("Renamed", true, permission.ParentId, false, 0));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var role = await CreateRoleAsync(client);
        await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([permission.Id]));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/permissions/{permission.Id}")).StatusCode);

        await client.PutAsJsonAsync($"/api/v1/roles/{role.Id}/permissions", new AssignPermissionsRequest([]));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/permissions/{permission.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_role_request_returns_400()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<RoleResponse> CreateRoleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(AuthTestHelper.UniqueCode("role"), "Test Role"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoleResponse>())!;
    }

    private static async Task<PermissionResponse> CreatePermissionAsync(HttpClient client)
    {
        var group = await PermissionHierarchyTests.CreateGroupAsync(client, null);
        return await PermissionHierarchyTests.CreateLeafAsync(client, group.Id);
    }

    private static async Task<UserDetailsResponse> CreateUserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest($"ac-{Guid.NewGuid():n}@integration.test", "Access Control User", "S3curePassw0rd!"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetailsResponse>())!;
    }
}

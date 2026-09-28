using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Organization.Application.Brands;
using AdminPlatform.Modules.Organization.Application.Departments;
using AdminPlatform.Modules.Organization.Application.Organizations;
using AdminPlatform.Modules.Organization.Application.UserScopes;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class OrganizationCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public OrganizationCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organization_can_be_created_read_and_updated_and_codes_are_unique()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organization = await CreateOrganizationAsync(client);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest(organization.Code, "Duplicate"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/organizations/{organization.Id}", new UpdateOrganizationRequest("Renamed Org", false));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<OrganizationResponse>())!;
        Assert.Equal("Renamed Org", updated.Name);
        Assert.False(updated.IsActive);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/organizations/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest("", ""))).StatusCode);
    }

    [Fact]
    public async Task Departments_form_a_tree_and_a_department_cannot_move_under_its_own_descendant()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organization = await CreateOrganizationAsync(client);
        var root = await CreateDepartmentAsync(client, organization.Id, "root", parentId: null);
        var child = await CreateDepartmentAsync(client, organization.Id, "child", root.Id);

        var tree = await client.GetFromJsonAsync<List<DepartmentTreeNode>>($"/api/v1/departments/tree?organizationId={organization.Id}");
        var rootNode = Assert.Single(tree!);
        Assert.Equal("root", rootNode.Code);
        Assert.Equal("child", Assert.Single(rootNode.Children).Code);

        var cycleResponse = await client.PutAsJsonAsync($"/api/v1/departments/{root.Id}", new UpdateDepartmentRequest("Root", true, child.Id));
        Assert.Equal(HttpStatusCode.BadRequest, cycleResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/departments", new CreateDepartmentRequest(organization.Id, "root", "Dup", null));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var moveResponse = await client.PutAsJsonAsync($"/api/v1/departments/{child.Id}", new UpdateDepartmentRequest("Child", true, null));
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
    }

    [Fact]
    public async Task A_department_parent_must_belong_to_the_same_organization()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organizationA = await CreateOrganizationAsync(client);
        var organizationB = await CreateOrganizationAsync(client);
        var parentInA = await CreateDepartmentAsync(client, organizationA.Id, "parent", parentId: null);

        var response = await client.PostAsJsonAsync("/api/v1/departments",
            new CreateDepartmentRequest(organizationB.Id, "child", "Child", parentInA.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Brand_can_be_created_read_and_updated_and_codes_are_unique_per_organization()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organization = await CreateOrganizationAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/brands", new CreateBrandRequest(organization.Id, "brand", "Brand"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var brand = (await createResponse.Content.ReadFromJsonAsync<BrandResponse>())!;

        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/v1/brands", new CreateBrandRequest(organization.Id, "brand", "Dup"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/brands/{brand.Id}")).StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/brands/{brand.Id}", new UpdateBrandRequest("Renamed Brand", true));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Renamed Brand", (await updateResponse.Content.ReadFromJsonAsync<BrandResponse>())!.Name);
    }

    [Fact]
    public async Task A_user_can_be_scoped_to_departments_and_brands_and_unscoped_again()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organization = await CreateOrganizationAsync(client);
        var department = await CreateDepartmentAsync(client, organization.Id, "dept", parentId: null);
        var brand = (await (await client.PostAsJsonAsync("/api/v1/brands", new CreateBrandRequest(organization.Id, "brand", "Brand")))
            .Content.ReadFromJsonAsync<BrandResponse>())!;
        var user = (await (await client.PostAsJsonAsync("/api/v1/users",
                new CreateUserRequest($"scope-{Guid.NewGuid():n}@integration.test", "Scoped User", "S3curePassw0rd!")))
            .Content.ReadFromJsonAsync<UserDetailsResponse>())!;

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsJsonAsync($"/api/v1/users/{user.Id}/departments", new AssignDepartmentRequest(department.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsJsonAsync($"/api/v1/users/{user.Id}/brands", new AssignBrandRequest(brand.Id))).StatusCode);

        var departments = await client.GetFromJsonAsync<List<UserDepartmentResponse>>($"/api/v1/users/{user.Id}/departments");
        Assert.Contains(departments!, d => d.DepartmentId == department.Id);
        var brands = await client.GetFromJsonAsync<List<UserBrandResponse>>($"/api/v1/users/{user.Id}/brands");
        Assert.Contains(brands!, b => b.BrandId == brand.Id);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync($"/api/v1/users/{user.Id}/departments", new AssignDepartmentRequest(Guid.NewGuid()))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/users/{user.Id}/departments/{department.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/users/{user.Id}/brands/{brand.Id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<UserDepartmentResponse>>($"/api/v1/users/{user.Id}/departments"))!);
    }

    private static async Task<OrganizationResponse> CreateOrganizationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest(AuthTestHelper.UniqueCode("org"), "Test Org"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganizationResponse>())!;
    }

    private static async Task<DepartmentResponse> CreateDepartmentAsync(HttpClient client, Guid organizationId, string code, Guid? parentId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/departments", new CreateDepartmentRequest(organizationId, code, code, parentId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DepartmentResponse>())!;
    }
}

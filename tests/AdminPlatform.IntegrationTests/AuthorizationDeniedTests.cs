using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class AuthorizationDeniedTests
{
    private readonly AdminPlatformApiFactory _factory;

    public AuthorizationDeniedTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_request_to_a_protected_endpoint_returns_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_without_the_required_permission_returns_403()
    {
        using var adminClient = _factory.CreateClient();
        var adminToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(adminClient, _factory.AdminEmail, _factory.AdminPassword);
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        // A freshly created user has no roles/permissions at all.
        var email = $"no-permissions-{Guid.NewGuid():n}@integration.test";
        const string password = "S3curePassw0rd!";
        await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "No Permissions", password));

        using var plainClient = _factory.CreateClient();
        var plainToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(plainClient, email, password);
        plainClient.DefaultRequestHeaders.Authorization = new("Bearer", plainToken);

        var response = await plainClient.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_bearer_token_is_rejected_as_401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "this-is-not-a-valid-jwt");

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

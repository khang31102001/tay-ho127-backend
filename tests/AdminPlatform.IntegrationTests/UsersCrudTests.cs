using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Auth;
using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class UsersCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public UsersCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var accessToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Admin_can_create_list_and_fetch_a_user()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var email = $"crud-{Guid.NewGuid():n}@integration.test";

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "CRUD Test User", "S3curePassw0rd!"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<UserDetailsResponse>();
        Assert.Equal(email, created!.Email);

        var getResponse = await client.GetAsync($"/api/v1/users/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/users?search=" + Uri.EscapeDataString(email));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<UserResponse>>();
        Assert.Contains(page!.Items, u => u.Email == email);
    }

    [Fact]
    public async Task Creating_a_duplicate_email_returns_409_with_a_problem_details_body()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var email = $"dup-{Guid.NewGuid():n}@integration.test";
        await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "First", "S3curePassw0rd!"));

        var secondResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "Second", "S3curePassw0rd!"));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_create_request_returns_400_with_field_errors()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest("not-an-email", "", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_newly_created_user_can_immediately_log_in_with_their_password()
    {
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var email = $"newlogin-{Guid.NewGuid():n}@integration.test";
        const string password = "S3curePassw0rd!";
        await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "New Login User", password));

        using var anonymousClient = _factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password, null));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_can_deactivate_a_user_and_they_can_no_longer_log_in()
    {
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var email = $"deactivate-{Guid.NewGuid():n}@integration.test";
        const string password = "S3curePassw0rd!";
        var created = (await (await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "To Deactivate", password)))
            .Content.ReadFromJsonAsync<UserDetailsResponse>())!;

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/users/{created.Id}", new UpdateUserRequest("To Deactivate", false));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var anonymousClient = _factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password, null));
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
}

internal sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

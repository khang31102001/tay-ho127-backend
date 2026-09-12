using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Customer.Application.Auth;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class CustomerAuthFlowTests
{
    private readonly AdminPlatformApiFactory _factory;

    public CustomerAuthFlowTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_returns_a_token_pair_for_a_new_customer()
    {
        using var client = _factory.CreateClient();
        var email = $"customer-{Guid.NewGuid():n}@integration.test";

        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane Doe", $"09{Guid.NewGuid():n}"[..10], email, "S3curePassw0rd!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<CustomerTokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email()
    {
        using var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():n}@integration.test";
        var phone1 = $"09{Guid.NewGuid():n}"[..10];
        var phone2 = $"09{Guid.NewGuid():n}"[..10];

        await CustomerAuthTestHelper.RegisterAsync(client, "First", phone1, email, "S3curePassw0rd!");
        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Second", phone2, email, "S3curePassw0rd!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_phone()
    {
        using var client = _factory.CreateClient();
        var phone = $"09{Guid.NewGuid():n}"[..10];

        await CustomerAuthTestHelper.RegisterAsync(client, "First", phone, $"a-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");
        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Second", phone, $"b-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_correct_password_succeeds()
    {
        using var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():n}@integration.test";
        await CustomerAuthTestHelper.RegisterAsync(client, "Jane", $"09{Guid.NewGuid():n}"[..10], email, "S3curePassw0rd!");

        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/login", new CustomerLoginRequest(email, "S3curePassw0rd!", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        using var client = _factory.CreateClient();
        var email = $"wrong-{Guid.NewGuid():n}@integration.test";
        await CustomerAuthTestHelper.RegisterAsync(client, "Jane", $"09{Guid.NewGuid():n}"[..10], email, "S3curePassw0rd!");

        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/login", new CustomerLoginRequest(email, "wrong-password", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_detects_reuse()
    {
        using var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"refresh-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/customer/auth/refresh", new CustomerRefreshTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<CustomerTokenResponse>();
        Assert.NotEqual(tokens.RefreshToken, refreshed!.RefreshToken);

        var reuse = await client.PostAsJsonAsync("/api/v1/customer/auth/refresh", new CustomerRefreshTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        using var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"logout-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/customer/auth/logout", new CustomerRefreshTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/customer/auth/refresh", new CustomerRefreshTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_authenticated_customers_profile()
    {
        using var client = _factory.CreateClient();
        var email = $"me-{Guid.NewGuid():n}@integration.test";
        var tokens = await CustomerAuthTestHelper.RegisterAsync(client, "Jane", $"09{Guid.NewGuid():n}"[..10], email, "S3curePassw0rd!");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/v1/customers/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<CustomerMeResponse>();
        Assert.Equal(email, me!.Email);
    }
}

using System.Net;

namespace AdminPlatform.IntegrationTests;

/// <summary>Verifies the §7 security boundary: an Admin-issued JWT and a Customer-issued JWT are validated
/// by the same JWT bearer scheme (same issuer/audience/signing key), but the account_type claim keeps them
/// from ever being accepted on the other side.</summary>
[Collection("Api")]
public class CrossRealmAuthorizationTests
{
    private readonly AdminPlatformApiFactory _factory;

    public CrossRealmAuthorizationTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_customer_token_cannot_call_the_admin_Me_endpoint()
    {
        using var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(
            client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"boundary-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_token_cannot_call_admin_logout_all()
    {
        using var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(
            client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"boundary2-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var response = await client.PostAsync("/api/v1/auth/logout-all", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_token_cannot_call_a_permission_gated_admin_endpoint()
    {
        using var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(
            client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"boundary3-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_token_cannot_call_the_customer_profile_endpoint()
    {
        using var client = _factory.CreateClient();
        var adminToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var response = await client.GetAsync("/api/v1/customers/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

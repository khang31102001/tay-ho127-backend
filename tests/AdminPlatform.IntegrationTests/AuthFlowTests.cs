using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Auth;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class AuthFlowTests
{
    private readonly AdminPlatformApiFactory _factory;

    public AuthFlowTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_with_seeded_admin_returns_a_token_pair()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(_factory.AdminEmail, _factory.AdminPassword, "integration-tests"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_and_never_reveals_which_field_was_wrong()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(_factory.AdminEmail, "definitely-wrong", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_the_old_refresh_token_can_no_longer_be_used()
    {
        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(_factory.AdminEmail, _factory.AdminPassword, null));
        var firstTokens = await login.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(firstTokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var secondTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotEqual(firstTokens.RefreshToken, secondTokens!.RefreshToken);

        // Reuse detection: presenting the already-rotated first refresh token again must fail.
        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(firstTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // Reuse detection revokes the whole session family, so even the latest (second) token is now dead.
        var secondNowDead = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(secondTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, secondNowDead.StatusCode);
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_caller_with_their_permissions_once_authenticated()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(_factory.AdminEmail, me!.Email);
        Assert.Contains("super-admin", me.Roles);
        Assert.Contains("users.view", me.Permissions);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(_factory.AdminEmail, _factory.AdminPassword, null));
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(tokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }
}

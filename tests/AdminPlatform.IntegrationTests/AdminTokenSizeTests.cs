using System.IdentityModel.Tokens.Jwt;
using System.Net;
using AdminPlatform.Common.Security;

namespace AdminPlatform.IntegrationTests;

/// <summary>The admin access token must stay small: it travels in cookies through a reverse proxy, and listing every permission
/// in it (~115 codes for SuperAdmin) overflowed the proxy's header buffer and approached the browsers' 4 KB cookie limit.</summary>
[Collection("Api")]
public class AdminTokenSizeTests
{
    private readonly AdminPlatformApiFactory _factory;

    public AdminTokenSizeTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Super_admin_token_carries_no_permission_claims_and_stays_small()
    {
        using var client = _factory.CreateClient();

        var accessToken = await AuthTestHelper.LoginAndGetAccessTokenAsync(client, _factory.AdminEmail, _factory.AdminPassword);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == AppClaimTypes.Permission);
        Assert.True(accessToken.Length < 1500, $"Access token is {accessToken.Length} characters long.");
    }

    [Fact]
    public async Task Super_admin_can_still_reach_permission_protected_endpoints_with_the_slim_token()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

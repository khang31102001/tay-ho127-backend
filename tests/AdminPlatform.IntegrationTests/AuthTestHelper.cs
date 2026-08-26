using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Auth;

namespace AdminPlatform.IntegrationTests;

internal static class AuthTestHelper
{
    public static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password, "integration-tests"));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }
}

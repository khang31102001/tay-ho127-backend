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

    /// <summary>A client already authenticated as the seeded SuperAdmin (every permission).</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(AdminPlatformApiFactory factory)
    {
        var client = factory.CreateClient();
        var accessToken = await LoginAndGetAccessTokenAsync(client, factory.AdminEmail, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        return client;
    }

    /// <summary>Unique short code for entities whose Code has a unique index.</summary>
    public static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid():n}"[..(prefix.Length + 13)];
}

using System.Net.Http.Json;
using AdminPlatform.Modules.Customer.Application.Auth;

namespace AdminPlatform.IntegrationTests;

internal static class CustomerAuthTestHelper
{
    public static async Task<CustomerTokenResponse> RegisterAsync(HttpClient client, string fullName, string phone, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest(fullName, phone, email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerTokenResponse>())!;
    }
}

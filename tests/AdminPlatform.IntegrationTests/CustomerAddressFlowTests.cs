using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Customer.Application.Addresses;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class CustomerAddressFlowTests
{
    private readonly AdminPlatformApiFactory _factory;

    public CustomerAddressFlowTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> NewAuthenticatedCustomerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await CustomerAuthTestHelper.RegisterAsync(
            client, "Jane", $"09{Guid.NewGuid():n}"[..10], $"addr-{Guid.NewGuid():n}@integration.test", "S3curePassw0rd!");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        return client;
    }

    [Fact]
    public async Task Creating_the_first_address_makes_it_the_default_even_when_not_requested()
    {
        using var client = await NewAuthenticatedCustomerClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers/me/addresses",
            new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CustomerAddressResponse>();
        Assert.True(created!.IsDefault);
    }

    [Fact]
    public async Task Only_one_address_can_be_default_at_a_time()
    {
        using var client = await NewAuthenticatedCustomerClientAsync();
        await client.PostAsJsonAsync("/api/v1/customers/me/addresses",
            new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true));
        var secondResponse = await client.PostAsJsonAsync("/api/v1/customers/me/addresses",
            new CreateCustomerAddressRequest("Jane", "0901234567", "456 Other St", null, null, null, null, IsDefault: true));
        var second = await secondResponse.Content.ReadFromJsonAsync<CustomerAddressResponse>();

        var listResponse = await client.GetAsync("/api/v1/customers/me/addresses");
        var all = await listResponse.Content.ReadFromJsonAsync<List<CustomerAddressResponse>>();

        Assert.Single(all!, a => a.IsDefault);
        Assert.True(all!.Single(a => a.Id == second!.Id).IsDefault);
    }

    [Fact]
    public async Task Delete_removes_the_address()
    {
        using var client = await NewAuthenticatedCustomerClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers/me/addresses",
            new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerAddressResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/customers/me/addresses/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/customers/me/addresses");
        var all = await listResponse.Content.ReadFromJsonAsync<List<CustomerAddressResponse>>();
        Assert.Empty(all!);
    }

    [Fact]
    public async Task A_customer_cannot_modify_another_customers_address()
    {
        using var ownerClient = await NewAuthenticatedCustomerClientAsync();
        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/customers/me/addresses",
            new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerAddressResponse>();

        using var attackerClient = await NewAuthenticatedCustomerClientAsync();
        var response = await attackerClient.DeleteAsync($"/api/v1/customers/me/addresses/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

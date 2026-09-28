using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Customer.Application.Addresses;
using AdminPlatform.Modules.Customer.Application.Customers;
using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class AdminCustomerTests
{
    private readonly AdminPlatformApiFactory _factory;

    public AdminCustomerTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_find_update_and_deactivate_a_customer()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var phone = UniquePhone();

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest("Walk-in Customer", phone, null, new DateOnly(1990, 1, 2), "female", null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var customer = (await createResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>())!;
        Assert.StartsWith("KH", customer.CustomerCode);
        Assert.Equal("Female", customer.Gender);
        Assert.Equal("Active", customer.Status);

        var page = await client.GetFromJsonAsync<PagedResultDto<CustomerProfileResponse>>($"/api/v1/customers?search={phone}");
        Assert.Contains(page!.Items, c => c.Id == customer.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/customers/{customer.Id}",
            new UpdateCustomerRequest("Renamed Customer", phone, null, null, null, "https://cdn.integration.test/a.png", IsActive: false));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>())!;
        Assert.Equal("Inactive", updated.Status);
        Assert.Equal("https://cdn.integration.test/a.png", updated.AvatarMediaId);

        var inactive = await client.GetFromJsonAsync<PagedResultDto<CustomerProfileResponse>>("/api/v1/customers?status=inactive&pageSize=200");
        Assert.Contains(inactive!.Items, c => c.Id == customer.Id);
    }

    [Fact]
    public async Task Creating_a_customer_with_a_phone_already_in_use_returns_409()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var phone = UniquePhone();
        await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest("First", phone, null, null, null, null));

        var response = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest("Second", phone, null, null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_customer_without_phone_or_email_returns_400()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest("No Contact", null, null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_manage_a_customers_addresses_and_only_one_stays_default()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var customer = (await (await client.PostAsJsonAsync("/api/v1/customers",
                new CreateCustomerRequest("Address Owner", UniquePhone(), null, null, null, null)))
            .Content.ReadFromJsonAsync<CustomerProfileResponse>())!;
        var basePath = $"/api/v1/customers/{customer.Id}/addresses";

        var first = await CreateAddressAsync(client, basePath, "1 First St");
        var second = await CreateAddressAsync(client, basePath, "2 Second St");
        Assert.True(first.IsDefault);
        Assert.False(second.IsDefault);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsync($"{basePath}/{second.Id}/default", null)).StatusCode);
        var addresses = await client.GetFromJsonAsync<List<CustomerAddressResponse>>(basePath);
        Assert.Equal(second.Id, Assert.Single(addresses!, a => a.IsDefault).Id);

        var updateResponse = await client.PutAsJsonAsync($"{basePath}/{first.Id}",
            new UpdateCustomerAddressRequest("Receiver", "0900000000", "1 Renamed St", null, null, null, "gate B"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("1 Renamed St", (await client.GetFromJsonAsync<CustomerAddressResponse>($"{basePath}/{first.Id}"))!.AddressLine);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{basePath}/{second.Id}")).StatusCode);
        var remaining = await client.GetFromJsonAsync<List<CustomerAddressResponse>>(basePath);
        Assert.True(Assert.Single(remaining!).IsDefault);
    }

    [Fact]
    public async Task An_address_of_another_customer_is_not_found()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var owner = (await (await client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest("Owner", UniquePhone(), null, null, null, null))).Content.ReadFromJsonAsync<CustomerProfileResponse>())!;
        var other = (await (await client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest("Other", UniquePhone(), null, null, null, null))).Content.ReadFromJsonAsync<CustomerProfileResponse>())!;
        var address = await CreateAddressAsync(client, $"/api/v1/customers/{owner.Id}/addresses", "1 Owner St");

        var response = await client.GetAsync($"/api/v1/customers/{other.Id}/addresses/{address.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Customer_admin_endpoints_require_the_customer_permissions()
    {
        using var adminClient = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var email = $"no-customer-perms-{Guid.NewGuid():n}@integration.test";
        await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(email, "No Perms", "S3curePassw0rd!"));

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await AuthTestHelper.LoginAndGetAccessTokenAsync(client, email, "S3curePassw0rd!"));

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/v1/customers")).StatusCode);
    }

    private static async Task<CustomerAddressResponse> CreateAddressAsync(HttpClient client, string basePath, string addressLine)
    {
        var response = await client.PostAsJsonAsync(basePath,
            new CreateCustomerAddressRequest("Receiver", "0900000000", addressLine, null, null, null, null, IsDefault: false));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CustomerAddressResponse>())!;
    }

    private static string UniquePhone() => "09" + Random.Shared.NextInt64(10_000_000, 99_999_999);
}

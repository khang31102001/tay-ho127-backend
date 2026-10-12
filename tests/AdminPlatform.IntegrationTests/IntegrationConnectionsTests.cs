using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Integration.Application.Connections;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class IntegrationConnectionsTests
{
    private readonly AdminPlatformApiFactory _factory;

    public IntegrationConnectionsTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_callers_cannot_read_integrations()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/integrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_seeded_esms_connection_is_listed_disabled_without_exposing_secrets()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/integrations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var connections = (await response.Content.ReadFromJsonAsync<List<IntegrationConnectionResponse>>())!;
        var esms = Assert.Single(connections, c => c.ProviderCode == "esms");
        Assert.False(esms.IsEnabled);
        Assert.Equal("sandbox", esms.Environment);
        Assert.Contains("apiKey", esms.MissingSecrets);
    }

    [Fact]
    public async Task Settings_can_be_saved_but_an_unknown_key_or_enabling_without_secrets_is_refused()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var esms = (await client.GetFromJsonAsync<List<IntegrationConnectionResponse>>("/api/v1/integrations"))!.Single(c => c.ProviderCode == "esms");

        var saved = await client.PutAsJsonAsync($"/api/v1/integrations/{esms.Id}",
            new UpdateIntegrationConnectionRequest("eSMS", false, "sandbox", new Dictionary<string, string> { ["oaId"] = "123456" }));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal("123456", (await saved.Content.ReadFromJsonAsync<IntegrationConnectionResponse>())!.Config["oaId"]);

        var unknownKey = await client.PutAsJsonAsync($"/api/v1/integrations/{esms.Id}",
            new UpdateIntegrationConnectionRequest("eSMS", false, "sandbox", new Dictionary<string, string> { ["apiKey"] = "leak" }));
        Assert.Equal(HttpStatusCode.BadRequest, unknownKey.StatusCode);

        var enableWithoutSecrets = await client.PutAsJsonAsync($"/api/v1/integrations/{esms.Id}",
            new UpdateIntegrationConnectionRequest("eSMS", true, "sandbox", null));
        Assert.Equal(HttpStatusCode.BadRequest, enableWithoutSecrets.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/integrations/{Guid.NewGuid()}")).StatusCode);
    }
}

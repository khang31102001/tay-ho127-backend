using AdminPlatform.Modules.Integration.Application.Connections;
using AdminPlatform.Modules.Integration.Application.Providers;
using AdminPlatform.Modules.Integration.Domain;
using AdminPlatform.Modules.Integration.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.UnitTests.Integration;

file sealed class FakeSecretSource : IIntegrationSecretSource
{
    private readonly Dictionary<string, string> _secrets;

    public FakeSecretSource(Dictionary<string, string>? secrets = null) => _secrets = secrets ?? [];

    public IReadOnlyDictionary<string, string> GetSecrets(IntegrationProviderDefinition provider) => _secrets;
}

public class IntegrationConnectionTests
{
    private static readonly Dictionary<string, string> AllEsmsSecrets = new()
    {
        ["apiKey"] = "key-value-123",
        ["secretKey"] = "secret-value-456",
    };

    private static IntegrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<IntegrationConnection> SeedEsmsAsync(IntegrationDbContext db)
    {
        var services = new ServiceCollection().AddSingleton<AdminPlatform.Modules.Integration.Application.IIntegrationDbContext>(db).BuildServiceProvider();
        await IntegrationSeeder.SeedAsync(services, CancellationToken.None);
        return await db.IntegrationConnections.SingleAsync(c => c.ProviderCode == IntegrationProviders.Esms);
    }

    [Fact]
    public async Task Seeder_creates_one_disabled_sandbox_connection_per_provider_and_is_idempotent()
    {
        var db = NewDb();
        var services = new ServiceCollection().AddSingleton<AdminPlatform.Modules.Integration.Application.IIntegrationDbContext>(db).BuildServiceProvider();

        var first = await IntegrationSeeder.SeedAsync(services, CancellationToken.None);
        var second = await IntegrationSeeder.SeedAsync(services, CancellationToken.None);

        Assert.Equal(IntegrationProviders.All.Count, first);
        Assert.Equal(0, second);
        var esms = await db.IntegrationConnections.SingleAsync();
        Assert.False(esms.IsEnabled);
        Assert.Equal(IntegrationEnvironment.Sandbox, esms.Environment);
        Assert.Equal("{}", esms.ConfigJson);
    }

    [Fact]
    public async Task List_never_exposes_secret_values_and_reports_which_are_missing()
    {
        var db = NewDb();
        await SeedEsmsAsync(db);
        var sut = new IntegrationConnectionService(db, new FakeSecretSource(new Dictionary<string, string> { ["apiKey"] = "key-value-123" }));

        var connections = await sut.ListAsync(CancellationToken.None);

        var esms = Assert.Single(connections);
        Assert.False(esms.SecretsConfigured);
        Assert.Equal(["secretKey"], esms.MissingSecrets);
        Assert.DoesNotContain("key-value-123", System.Text.Json.JsonSerializer.Serialize(esms));
    }

    [Fact]
    public async Task Update_saves_normalized_settings_and_a_case_insensitive_environment()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);
        var sut = new IntegrationConnectionService(db, new FakeSecretSource());

        var response = await sut.UpdateAsync(
            connection.Id,
            new UpdateIntegrationConnectionRequest(
                "  eSMS  ", false, "LIVE",
                new Dictionary<string, string> { ["oaId"] = "  123  ", ["brandname"] = "   " }),
            CancellationToken.None);

        Assert.Equal("eSMS", response.DisplayName);
        Assert.Equal("live", response.Environment);
        Assert.Equal("123", response.Config["oaId"]);
        Assert.DoesNotContain("brandname", response.Config.Keys);
    }

    [Fact]
    public async Task Update_rejects_a_setting_key_the_provider_does_not_know()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);
        var sut = new IntegrationConnectionService(db, new FakeSecretSource());

        var exception = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(
            connection.Id,
            new UpdateIntegrationConnectionRequest("eSMS", false, "sandbox", new Dictionary<string, string> { ["apiKey"] = "oops" }),
            CancellationToken.None));

        Assert.Contains("apiKey", exception.Message);
    }

    [Fact]
    public async Task Update_rejects_an_unknown_environment()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);
        var sut = new IntegrationConnectionService(db, new FakeSecretSource());

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(
            connection.Id, new UpdateIntegrationConnectionRequest("eSMS", false, "staging", null), CancellationToken.None));
    }

    [Fact]
    public async Task Enabling_is_refused_while_a_secret_is_missing_and_allowed_once_all_are_set()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);

        var withoutSecrets = new IntegrationConnectionService(db, new FakeSecretSource());
        var exception = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => withoutSecrets.UpdateAsync(
            connection.Id, new UpdateIntegrationConnectionRequest("eSMS", true, "sandbox", null), CancellationToken.None));
        Assert.Contains("apiKey", exception.Message);
        Assert.Contains("secretKey", exception.Message);
        Assert.False((await db.IntegrationConnections.SingleAsync()).IsEnabled);

        var withSecrets = new IntegrationConnectionService(db, new FakeSecretSource(AllEsmsSecrets));
        var response = await withSecrets.UpdateAsync(
            connection.Id, new UpdateIntegrationConnectionRequest("eSMS", true, "sandbox", null), CancellationToken.None);
        Assert.True(response.IsEnabled);
        Assert.True(response.SecretsConfigured);
    }

    [Fact]
    public async Task Disabling_never_needs_the_secrets()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);
        var sut = new IntegrationConnectionService(db, new FakeSecretSource());

        var response = await sut.UpdateAsync(
            connection.Id, new UpdateIntegrationConnectionRequest("eSMS", false, "sandbox", null), CancellationToken.None);

        Assert.False(response.IsEnabled);
    }

    [Fact]
    public async Task GetById_throws_NotFound_for_an_unknown_id()
    {
        var sut = new IntegrationConnectionService(NewDb(), new FakeSecretSource());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Query_service_returns_settings_and_secrets_but_its_text_form_hides_them()
    {
        var db = NewDb();
        var connection = await SeedEsmsAsync(db);
        var admin = new IntegrationConnectionService(db, new FakeSecretSource(AllEsmsSecrets));
        await admin.UpdateAsync(
            connection.Id,
            new UpdateIntegrationConnectionRequest("eSMS", true, "sandbox", new Dictionary<string, string> { ["oaId"] = "999" }),
            CancellationToken.None);
        var sut = new IntegrationConnectionQueryService(db, new FakeSecretSource(AllEsmsSecrets));

        var snapshot = await sut.FindAsync("ESMS", CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.True(snapshot.IsEnabled);
        Assert.Equal(IntegrationEnvironment.Sandbox, snapshot.Environment);
        Assert.Equal("999", snapshot.Config["oaId"]);
        Assert.Equal("key-value-123", snapshot.Secrets["apiKey"]);
        Assert.DoesNotContain("key-value-123", snapshot.ToString());
        Assert.DoesNotContain("secret-value-456", snapshot.ToString());
    }

    [Fact]
    public async Task Query_service_returns_null_for_an_unknown_provider_or_a_missing_row()
    {
        var sut = new IntegrationConnectionQueryService(NewDb(), new FakeSecretSource(AllEsmsSecrets));

        Assert.Null(await sut.FindAsync("nope", CancellationToken.None));
        Assert.Null(await sut.FindAsync(IntegrationProviders.Esms, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "sandbox", false)]
    [InlineData("eSMS", "staging", false)]
    [InlineData("eSMS", "", false)]
    [InlineData("eSMS", "live", true)]
    [InlineData("eSMS", "Sandbox", true)]
    public void Update_request_validator_checks_name_and_environment(string displayName, string environment, bool expectedValid)
    {
        var result = new UpdateIntegrationConnectionRequestValidator()
            .Validate(new UpdateIntegrationConnectionRequest(displayName, false, environment, null));

        Assert.Equal(expectedValid, result.IsValid);
    }
}

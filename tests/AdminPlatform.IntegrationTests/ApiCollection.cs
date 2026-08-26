namespace AdminPlatform.IntegrationTests;

/// <summary>Shares one Postgres container + WebApplicationFactory across every test class in this
/// collection instead of spinning up a fresh container per class.</summary>
[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<AdminPlatformApiFactory>;

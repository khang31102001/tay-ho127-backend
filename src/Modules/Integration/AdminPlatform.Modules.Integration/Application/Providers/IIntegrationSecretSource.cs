namespace AdminPlatform.Modules.Integration.Application.Providers;

/// <summary>Where a provider's secrets come from. Today that is server configuration (environment variables); a
/// different source (an encrypted column, a vault) can replace it without touching anything that uses a connection.</summary>
public interface IIntegrationSecretSource
{
    /// <summary>The secrets that are actually set, by short name (e.g. "apiKey"). A secret that is empty or missing is left out.</summary>
    IReadOnlyDictionary<string, string> GetSecrets(IntegrationProviderDefinition provider);
}

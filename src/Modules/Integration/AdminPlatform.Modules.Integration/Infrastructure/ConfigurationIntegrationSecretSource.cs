using AdminPlatform.Modules.Integration.Application.Providers;
using Microsoft.Extensions.Configuration;

namespace AdminPlatform.Modules.Integration.Infrastructure;

/// <summary>Reads secrets from server configuration, e.g. the environment variable Integrations__Esms__ApiKey.</summary>
internal sealed class ConfigurationIntegrationSecretSource : IIntegrationSecretSource
{
    private readonly IConfiguration _configuration;

    public ConfigurationIntegrationSecretSource(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyDictionary<string, string> GetSecrets(IntegrationProviderDefinition provider)
    {
        var secrets = new Dictionary<string, string>();
        foreach (var (name, path) in provider.SecretConfigPaths)
        {
            var value = _configuration[path];
            if (!string.IsNullOrWhiteSpace(value))
            {
                secrets[name] = value;
            }
        }

        return secrets;
    }
}

using AdminPlatform.Modules.Integration.Domain;

namespace AdminPlatform.Modules.Integration.Application.Connections;

/// <summary>A connection together with its resolved secrets, for in-process use by the code that actually calls the
/// provider (through a Host adapter). It must never be returned from an API or written to a log: ToString hides the
/// secrets, but treat the object itself as sensitive.</summary>
public sealed class IntegrationConnectionSnapshot
{
    public IntegrationConnectionSnapshot(
        string providerCode,
        bool isEnabled,
        IntegrationEnvironment environment,
        IReadOnlyDictionary<string, string> config,
        IReadOnlyDictionary<string, string> secrets)
    {
        ProviderCode = providerCode;
        IsEnabled = isEnabled;
        Environment = environment;
        Config = config;
        Secrets = secrets;
    }

    public string ProviderCode { get; }
    public bool IsEnabled { get; }
    public IntegrationEnvironment Environment { get; }
    public IReadOnlyDictionary<string, string> Config { get; }
    public IReadOnlyDictionary<string, string> Secrets { get; }

    public override string ToString() => $"IntegrationConnection({ProviderCode}, enabled={IsEnabled}, {Environment}, secrets hidden)";
}

/// <summary>What other modules need from Integration: the settings and secrets of one provider. Consumed only
/// through an adapter in the Host, never referenced by another module directly.</summary>
public interface IIntegrationConnectionQueryService
{
    /// <summary>Null when the provider is unknown or has no connection row. A disabled connection is still returned
    /// (IsEnabled false) so the caller can tell "off" from "not set up".</summary>
    Task<IntegrationConnectionSnapshot?> FindAsync(string providerCode, CancellationToken cancellationToken);
}

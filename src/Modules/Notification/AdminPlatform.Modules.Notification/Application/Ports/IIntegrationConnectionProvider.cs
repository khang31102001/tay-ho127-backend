namespace AdminPlatform.Modules.Notification.Application.Ports;

/// <summary>How to reach one third-party provider: whether it is switched on, whether it is the test or the real
/// side, its non-secret settings and its secrets. Notification defines this port; the Host implements it with the
/// Integration module, so Notification never references that module.</summary>
public interface IIntegrationConnectionProvider
{
    /// <summary>Null when the provider has no connection at all. A switched-off connection is still returned
    /// (<see cref="ProviderConnection.IsEnabled"/> false) so the caller can tell "off" from "not set up".</summary>
    Task<ProviderConnection?> FindAsync(string providerCode, CancellationToken cancellationToken);
}

/// <summary>Holds secrets: use it, never log it or return it from an API. Its text form hides them.</summary>
public sealed class ProviderConnection
{
    public ProviderConnection(
        string providerCode,
        bool isEnabled,
        bool isSandbox,
        IReadOnlyDictionary<string, string> settings,
        IReadOnlyDictionary<string, string> secrets)
    {
        ProviderCode = providerCode;
        IsEnabled = isEnabled;
        IsSandbox = isSandbox;
        Settings = settings;
        Secrets = secrets;
    }

    public string ProviderCode { get; }
    public bool IsEnabled { get; }

    /// <summary>True when messages go to the provider's test mode: nothing reaches a real phone and nothing is billed.</summary>
    public bool IsSandbox { get; }

    public IReadOnlyDictionary<string, string> Settings { get; }
    public IReadOnlyDictionary<string, string> Secrets { get; }

    public override string ToString() => $"ProviderConnection({ProviderCode}, enabled={IsEnabled}, sandbox={IsSandbox}, secrets hidden)";
}

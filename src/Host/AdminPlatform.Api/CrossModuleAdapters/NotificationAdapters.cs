using AdminPlatform.Modules.Integration.Application.Connections;
using AdminPlatform.Modules.Integration.Domain;
using AdminPlatform.Modules.Notification.Application.Ports;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Notification's IIntegrationConnectionProvider port with the Integration module's connection
/// settings and secrets. Lives in the Host because only the Host may reference both modules (architecture
/// assumption #6). The object it returns holds secrets and is only ever handed to the message sender.</summary>
internal sealed class NotificationIntegrationConnectionAdapter : IIntegrationConnectionProvider
{
    private readonly IIntegrationConnectionQueryService _connections;

    public NotificationIntegrationConnectionAdapter(IIntegrationConnectionQueryService connections)
    {
        _connections = connections;
    }

    public async Task<ProviderConnection?> FindAsync(string providerCode, CancellationToken cancellationToken)
    {
        var snapshot = await _connections.FindAsync(providerCode, cancellationToken);
        return snapshot is null
            ? null
            : new ProviderConnection(
                snapshot.ProviderCode,
                snapshot.IsEnabled,
                snapshot.Environment == IntegrationEnvironment.Sandbox,
                snapshot.Config,
                snapshot.Secrets);
    }
}

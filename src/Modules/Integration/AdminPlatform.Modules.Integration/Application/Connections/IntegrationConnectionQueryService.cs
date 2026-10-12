using AdminPlatform.Modules.Integration.Application.Providers;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Integration.Application.Connections;

public sealed class IntegrationConnectionQueryService : IIntegrationConnectionQueryService
{
    private readonly IIntegrationDbContext _db;
    private readonly IIntegrationSecretSource _secrets;

    public IntegrationConnectionQueryService(IIntegrationDbContext db, IIntegrationSecretSource secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    public async Task<IntegrationConnectionSnapshot?> FindAsync(string providerCode, CancellationToken cancellationToken)
    {
        var provider = IntegrationProviders.Find(providerCode);
        if (provider is null)
        {
            return null;
        }

        var connection = await _db.IntegrationConnections.AsNoTracking()
            .SingleOrDefaultAsync(c => c.ProviderCode == provider.Code, cancellationToken);
        if (connection is null)
        {
            return null;
        }

        return new IntegrationConnectionSnapshot(
            connection.ProviderCode,
            connection.IsEnabled,
            connection.Environment,
            IntegrationConnectionMapper.ReadConfig(connection.ConfigJson),
            _secrets.GetSecrets(provider));
    }
}

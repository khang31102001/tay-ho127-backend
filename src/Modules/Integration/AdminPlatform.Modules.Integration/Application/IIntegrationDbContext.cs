using AdminPlatform.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Integration.Application;

/// <summary>Persistence port for the Integration module — Application depends on this, not on EF Core directly.</summary>
public interface IIntegrationDbContext
{
    DbSet<IntegrationConnection> IntegrationConnections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

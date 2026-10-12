using AdminPlatform.Modules.Integration.Application;
using AdminPlatform.Modules.Integration.Application.Providers;
using AdminPlatform.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Integration.Infrastructure;

/// <summary>Creates one connection row per known provider, DISABLED and in sandbox, so nothing can reach a real
/// customer until an admin switches it on once its secrets are in place. A provider that already has a row is left
/// untouched, so it is safe in the every-deploy `seed` step and never overwrites an admin's settings.</summary>
public static class IntegrationSeeder
{
    /// <returns>How many connections were created.</returns>
    public static async Task<int> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IIntegrationDbContext>();

        var existing = await db.IntegrationConnections.Select(c => c.ProviderCode).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var provider in IntegrationProviders.All.Where(p => !existing.Contains(p.Code, StringComparer.OrdinalIgnoreCase)))
        {
            db.IntegrationConnections.Add(IntegrationConnection.Create(provider.Code, provider.DisplayName, IntegrationEnvironment.Sandbox, "{}"));
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}

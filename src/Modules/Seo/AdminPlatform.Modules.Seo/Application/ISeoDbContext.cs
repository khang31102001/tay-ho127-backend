using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application;

/// <summary>Persistence port for the Seo module — Application depends on this, not on EF Core directly.
/// Implemented by SeoDbContext (Infrastructure).</summary>
public interface ISeoDbContext
{
    DbSet<SeoSettings> SeoSettings { get; }
    DbSet<SeoMetadata> SeoMetadata { get; }
    DbSet<Redirect> Redirects { get; }
    DbSet<SeoSchema> SeoSchemas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application;

/// <summary>Persistence port for the Catalog module — Application depends on this, not on EF Core directly.
/// Implemented by CatalogDbContext (Infrastructure).</summary>
public interface ICatalogDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<SalesMenu> SalesMenus { get; }
    DbSet<SalesMenuProduct> SalesMenuProducts { get; }
    DbSet<ModifierGroup> ModifierGroups { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

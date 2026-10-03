using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Infrastructure;

public sealed class CatalogDbContext : DbContext, ICatalogDbContext
{
    public const string Schema = "catalog";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SalesMenu> SalesMenus => Set<SalesMenu>();
    public DbSet<SalesMenuProduct> SalesMenuProducts => Set<SalesMenuProduct>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}

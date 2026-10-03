using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Infrastructure;

public sealed class SeoDbContext : DbContext, ISeoDbContext
{
    public const string Schema = "seo";

    public DbSet<SeoSettings> SeoSettings => Set<SeoSettings>();
    public DbSet<SeoMetadata> SeoMetadata => Set<SeoMetadata>();
    public DbSet<Redirect> Redirects => Set<Redirect>();
    public DbSet<SeoSchema> SeoSchemas => Set<SeoSchema>();

    public SeoDbContext(DbContextOptions<SeoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeoDbContext).Assembly);
    }
}

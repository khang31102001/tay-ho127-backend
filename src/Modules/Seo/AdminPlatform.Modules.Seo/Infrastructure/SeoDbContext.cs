using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Infrastructure;

public sealed class SeoDbContext : DbContext, ISeoDbContext
{
    public const string Schema = "seo";

    public DbSet<SeoSettings> SeoSettings => Set<SeoSettings>();

    public SeoDbContext(DbContextOptions<SeoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeoDbContext).Assembly);
    }
}

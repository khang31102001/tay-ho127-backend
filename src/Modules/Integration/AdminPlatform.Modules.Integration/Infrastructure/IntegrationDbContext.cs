using AdminPlatform.Modules.Integration.Application;
using AdminPlatform.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Integration.Infrastructure;

public sealed class IntegrationDbContext : DbContext, IIntegrationDbContext
{
    public const string Schema = "integration";

    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();

    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationDbContext).Assembly);
    }
}

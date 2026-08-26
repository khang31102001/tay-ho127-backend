using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.Organization.Infrastructure;

/// <summary>See AdminPlatform.Modules.Identity.Infrastructure.IdentityDbContextFactory for why this exists.</summary>
public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", OrganizationDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new OrganizationDbContext(optionsBuilder.Options);
    }
}

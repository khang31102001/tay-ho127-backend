using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

/// <summary>See AdminPlatform.Modules.Identity.Infrastructure.IdentityDbContextFactory for why this exists.</summary>
public sealed class NavigationDbContextFactory : IDesignTimeDbContextFactory<NavigationDbContext>
{
    public NavigationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<NavigationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", NavigationDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new NavigationDbContext(optionsBuilder.Options);
    }
}

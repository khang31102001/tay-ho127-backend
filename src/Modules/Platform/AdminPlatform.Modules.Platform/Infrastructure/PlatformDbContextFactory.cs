using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.Platform.Infrastructure;

/// <summary>See AdminPlatform.Modules.Identity.Infrastructure.IdentityDbContextFactory for why this exists.</summary>
public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", PlatformDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new PlatformDbContext(optionsBuilder.Options);
    }
}

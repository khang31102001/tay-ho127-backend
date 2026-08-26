using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>See AdminPlatform.Modules.Identity.Infrastructure.IdentityDbContextFactory for why this exists.</summary>
public sealed class AccessControlDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    public AccessControlDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", AccessControlDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new AccessControlDbContext(optionsBuilder.Options);
    }
}

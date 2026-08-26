using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.Identity.Infrastructure;

/// <summary>Lets `dotnet ef migrations add` build this module's DbContext directly, without executing the
/// Migrator's Program.cs (which parses CLI args like "migrate"/"seed" and would otherwise run as a side
/// effect of design-time discovery). The connection string only needs to be syntactically valid here —
/// `migrations add` never actually connects.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new IdentityDbContext(optionsBuilder.Options);
    }
}

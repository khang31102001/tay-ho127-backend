using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminPlatform.Modules.Notification.Infrastructure;

/// <summary>See AdminPlatform.Modules.Identity.Infrastructure.IdentityDbContextFactory for why this exists.</summary>
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=adminplatform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationDbContext.Schema));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new NotificationDbContext(optionsBuilder.Options);
    }
}

using AdminPlatform.Modules.Notification.Application;
using AdminPlatform.Modules.Notification.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Notification.Infrastructure;

public sealed class NotificationDbContext : DbContext, INotificationDbContext
{
    public const string Schema = "notification";

    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<DeliveryLog> DeliveryLogs => Set<DeliveryLog>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}

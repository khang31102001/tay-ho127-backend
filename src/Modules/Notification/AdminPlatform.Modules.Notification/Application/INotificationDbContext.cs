using AdminPlatform.Modules.Notification.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Notification.Application;

/// <summary>Persistence port for the Notification module — Application depends on this, not on EF Core directly.</summary>
public interface INotificationDbContext
{
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<DeliveryLog> DeliveryLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

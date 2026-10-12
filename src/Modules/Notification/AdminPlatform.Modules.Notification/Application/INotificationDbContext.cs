using AdminPlatform.Modules.Notification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AdminPlatform.Modules.Notification.Application;

/// <summary>Persistence port for the Notification module — Application depends on this, not on EF Core directly.</summary>
public interface INotificationDbContext
{
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<DeliveryLog> DeliveryLogs { get; }

    /// <summary>Needed to reload a challenge after a concurrent update (see <see cref="OtpChallenge.RowVersion"/>).</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

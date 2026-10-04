using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application;

/// <summary>Persistence port for the Sales module — Application depends on this, not on EF Core directly.
/// Implemented by SalesDbContext (Infrastructure).</summary>
public interface ISalesDbContext
{
    DbSet<DeliveryMethod> DeliveryMethods { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<OrderOptionGroup> OrderOptionGroups { get; }
    DbSet<OrderSettings> OrderSettings { get; }
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentSession> PaymentSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

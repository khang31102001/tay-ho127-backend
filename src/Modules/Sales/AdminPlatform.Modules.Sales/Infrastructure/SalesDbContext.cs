using AdminPlatform.Modules.Sales.Application;
using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Infrastructure;

public sealed class SalesDbContext : DbContext, ISalesDbContext
{
    public const string Schema = "sales";

    public DbSet<DeliveryMethod> DeliveryMethods => Set<DeliveryMethod>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<OrderOptionGroup> OrderOptionGroups => Set<OrderOptionGroup>();
    public DbSet<OrderSettings> OrderSettings => Set<OrderSettings>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentSession> PaymentSessions => Set<PaymentSession>();

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}

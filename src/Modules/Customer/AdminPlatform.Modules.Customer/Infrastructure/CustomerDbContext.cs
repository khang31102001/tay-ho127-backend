using AdminPlatform.Modules.Customer.Application;
using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.Modules.Customer.Infrastructure;

public sealed class CustomerDbContext : DbContext, ICustomerDbContext
{
    public const string Schema = "customer";

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerAuthIdentity> CustomerAuthIdentities => Set<CustomerAuthIdentity>();
    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();

    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
    }
}

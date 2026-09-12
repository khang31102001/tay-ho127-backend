using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application;

/// <summary>Persistence port for the Customer module — Application depends on this, not on EF Core
/// directly. Implemented by CustomerDbContext (Infrastructure). See architecture assumption #4.</summary>
public interface ICustomerDbContext
{
    DbSet<Domain.Customer> Customers { get; }
    DbSet<CustomerAddress> CustomerAddresses { get; }
    DbSet<CustomerAuthIdentity> CustomerAuthIdentities { get; }
    DbSet<CustomerRefreshToken> CustomerRefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

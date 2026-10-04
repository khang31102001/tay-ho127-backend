using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application.Customers;

/// <summary>The Customer module's small read contract for other modules (reached through a Host adapter): is this id a
/// real, usable customer account?</summary>
public interface ICustomerLookupService
{
    Task<bool> ExistsActiveAsync(Guid customerId, CancellationToken cancellationToken);
}

public sealed class CustomerLookupService : ICustomerLookupService
{
    private readonly ICustomerDbContext _db;

    public CustomerLookupService(ICustomerDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsActiveAsync(Guid customerId, CancellationToken cancellationToken) =>
        _db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId && c.Status == CustomerStatus.Active, cancellationToken);
}

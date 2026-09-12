using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application.Addresses;

public sealed class CustomerAddressService : ICustomerAddressService
{
    private readonly ICustomerDbContext _db;

    public CustomerAddressService(ICustomerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerAddressResponse>> ListAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var addresses = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync(cancellationToken);

        return addresses.Select(ToResponse).ToList();
    }

    public async Task<CustomerAddressResponse> CreateAsync(Guid customerId, CreateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var hasAnyAddress = await _db.CustomerAddresses.AnyAsync(a => a.CustomerId == customerId, cancellationToken);

        // Every customer with at least one address must have exactly one default — the first address is
        // always the default regardless of what the caller asked for.
        var isDefault = request.IsDefault || !hasAnyAddress;

        if (isDefault)
        {
            await UnsetCurrentDefaultAsync(customerId, cancellationToken);
        }

        var address = CustomerAddress.Create(
            customerId, request.ReceiverName, request.Phone, request.AddressLine,
            request.Ward, request.District, request.Province, request.AddressNote, isDefault);

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(address);
    }

    public async Task<CustomerAddressResponse> UpdateAsync(Guid customerId, Guid addressId, UpdateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await FindOwnedOrThrowAsync(customerId, addressId, cancellationToken);

        address.Update(request.ReceiverName, request.Phone, request.AddressLine, request.Ward, request.District, request.Province, request.AddressNote);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(address);
    }

    public async Task DeleteAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        var address = await FindOwnedOrThrowAsync(customerId, addressId, cancellationToken);
        var wasDefault = address.IsDefault;

        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);

        if (!wasDefault)
        {
            return;
        }

        // Keep the "at most one default, and always one if any address remains" invariant: promote an
        // arbitrary (but deterministic) remaining address — CustomerAddress carries no timestamp to pick
        // the most recent by.
        var replacement = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (replacement is not null)
        {
            replacement.MarkAsDefault();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<CustomerAddressResponse> SetDefaultAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        var address = await FindOwnedOrThrowAsync(customerId, addressId, cancellationToken);

        if (!address.IsDefault)
        {
            await UnsetCurrentDefaultAsync(customerId, cancellationToken);
            address.MarkAsDefault();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(address);
    }

    private async Task UnsetCurrentDefaultAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var currentDefaults = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var address in currentDefaults)
        {
            address.UnmarkAsDefault();
        }
    }

    private async Task<CustomerAddress> FindOwnedOrThrowAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken) =>
        await _db.CustomerAddresses.SingleOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), addressId);

    private static CustomerAddressResponse ToResponse(CustomerAddress address) => new(
        address.Id, address.ReceiverName, address.Phone, address.AddressLine,
        address.Ward, address.District, address.Province, address.AddressNote, address.IsDefault);
}

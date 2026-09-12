namespace AdminPlatform.Modules.Customer.Application.Addresses;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressResponse>> ListAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerAddressResponse> CreateAsync(Guid customerId, CreateCustomerAddressRequest request, CancellationToken cancellationToken);

    Task<CustomerAddressResponse> UpdateAsync(Guid customerId, Guid addressId, UpdateCustomerAddressRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken);

    Task<CustomerAddressResponse> SetDefaultAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken);
}

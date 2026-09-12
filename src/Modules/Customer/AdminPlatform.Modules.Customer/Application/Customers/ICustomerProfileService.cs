namespace AdminPlatform.Modules.Customer.Application.Customers;

public interface ICustomerProfileService
{
    Task<CustomerProfileResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateAvatarAsync(Guid customerId, UpdateCustomerAvatarRequest request, CancellationToken cancellationToken);
}

using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Customer.Application.Customers;

public interface ICustomerProfileService
{
    Task<PagedResult<CustomerProfileResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateAvatarAsync(Guid customerId, UpdateCustomerAvatarRequest request, CancellationToken cancellationToken);
}

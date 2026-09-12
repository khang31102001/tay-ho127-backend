using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application.Customers;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly ICustomerDbContext _db;

    public CustomerProfileService(ICustomerDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerProfileResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerProfileResponse> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);

        CustomerGender? gender = null;
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (!Enum.TryParse<CustomerGender>(request.Gender, ignoreCase: true, out var parsedGender))
            {
                throw new BusinessRuleValidationException($"'{request.Gender}' is not a valid gender.");
            }

            gender = parsedGender;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            var phoneTaken = await _db.Customers.AnyAsync(c => c.Id != customerId && c.Phone == normalizedPhone, cancellationToken);
            if (phoneTaken)
            {
                throw new ConflictException($"A customer with phone '{normalizedPhone}' already exists.");
            }
        }

        customer.UpdateProfile(request.FullName, request.Phone, request.Email, request.DateOfBirth, gender);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerProfileResponse> UpdateAvatarAsync(Guid customerId, UpdateCustomerAvatarRequest request, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);
        customer.UpdateAvatar(request.AvatarMediaId);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    private async Task<Domain.Customer> FindOrThrowAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _db.Customers.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customer), customerId);

    private static CustomerProfileResponse ToResponse(Domain.Customer customer) => new(
        customer.Id,
        customer.CustomerCode,
        customer.FullName,
        customer.Phone,
        customer.Email,
        customer.AvatarMediaId,
        customer.DateOfBirth,
        customer.Gender?.ToString(),
        customer.Status.ToString(),
        customer.CreatedAtUtc,
        customer.UpdatedAtUtc);
}

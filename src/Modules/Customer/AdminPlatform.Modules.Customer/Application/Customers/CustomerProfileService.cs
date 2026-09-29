using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application.Customers;

/// <summary>Customer profile operations, shared by the customer's own self-service endpoints
/// (/customers/me) and the admin endpoints (/customers). Both paths go through the same validation of
/// gender and phone/email uniqueness.</summary>
public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly ICustomerDbContext _db;

    public CustomerProfileService(ICustomerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerProfileResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = Enum.TryParse<CustomerStatus>(status, ignoreCase: true, out var value)
                ? value
                : throw new BusinessRuleValidationException($"'{status}' is not a valid status.");
            query = query.Where(c => c.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, pattern)
                || EF.Functions.ILike(c.CustomerCode, pattern)
                || (c.Phone != null && EF.Functions.ILike(c.Phone, pattern))
                || (c.Email != null && EF.Functions.ILike(c.Email, pattern)));
        }

        query = request.IsDescending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc);

        // Mapped after paging: Gender/Status are enums rendered via ToString(), which Npgsql cannot translate.
        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<CustomerProfileResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<CustomerProfileResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerProfileResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var gender = ParseGender(request.Gender);
        await EnsureContactAvailableAsync(customerId: null, request.Phone, request.Email, cancellationToken);

        var customer = Domain.Customer.Create(request.FullName, request.Phone, request.Email);
        customer.UpdateProfile(request.FullName, request.Phone, request.Email, request.DateOfBirth, gender);
        customer.UpdateAvatar(request.AvatarMediaId);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerProfileResponse> UpdateAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);
        var gender = ParseGender(request.Gender);
        await EnsureContactAvailableAsync(customerId, request.Phone, request.Email, cancellationToken);

        customer.UpdateProfile(request.FullName, request.Phone, request.Email, request.DateOfBirth, gender);
        customer.UpdateAvatar(request.AvatarMediaId);
        if (request.IsActive)
        {
            customer.Activate();
        }
        else
        {
            customer.Deactivate();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerProfileResponse> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var customer = await FindOrThrowAsync(customerId, cancellationToken);
        var gender = ParseGender(request.Gender);
        await EnsureContactAvailableAsync(customerId, request.Phone, request.Email, cancellationToken);

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

    private static CustomerGender? ParseGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return Enum.TryParse<CustomerGender>(gender, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{gender}' is not a valid gender.");
    }

    /// <summary>Phone and email are unique across customers (filtered unique indexes) — checked up front so
    /// the caller gets a readable 409 instead of a raw constraint violation.</summary>
    private async Task EnsureContactAvailableAsync(Guid? customerId, string? phone, string? email, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalizedPhone = phone.Trim();
            if (await _db.Customers.AnyAsync(c => c.Id != customerId && c.Phone == normalizedPhone, cancellationToken))
            {
                throw new ConflictException($"A customer with phone '{normalizedPhone}' already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _db.Customers.AnyAsync(c => c.Id != customerId && c.Email == normalizedEmail, cancellationToken))
            {
                throw new ConflictException($"A customer with email '{normalizedEmail}' already exists.");
            }
        }
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

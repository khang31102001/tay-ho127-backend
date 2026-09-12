namespace AdminPlatform.Modules.Customer.Application.Customers;

public sealed record UpdateCustomerProfileRequest(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender);

public sealed record UpdateCustomerAvatarRequest(string? AvatarMediaId);

public sealed record CustomerProfileResponse(
    Guid Id,
    string CustomerCode,
    string FullName,
    string? Phone,
    string? Email,
    string? AvatarMediaId,
    DateOnly? DateOfBirth,
    string? Gender,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

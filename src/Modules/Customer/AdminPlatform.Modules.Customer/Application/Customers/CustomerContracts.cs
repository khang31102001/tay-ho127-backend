namespace AdminPlatform.Modules.Customer.Application.Customers;

public sealed record UpdateCustomerProfileRequest(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender);

public sealed record UpdateCustomerAvatarRequest(string? AvatarMediaId);

/// <summary>Admin-created customer (e.g. a walk-in or phone order). No login credential is created — the
/// customer can later register/sign in with the same email or phone.</summary>
public sealed record CreateCustomerRequest(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender,
    string? AvatarMediaId);

/// <summary>Admin full update of a customer: profile, avatar and active status in one request.</summary>
public sealed record UpdateCustomerRequest(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender,
    string? AvatarMediaId,
    bool IsActive);

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

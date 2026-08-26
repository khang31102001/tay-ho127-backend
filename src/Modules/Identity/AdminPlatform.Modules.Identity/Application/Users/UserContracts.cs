namespace AdminPlatform.Modules.Identity.Application.Users;

public sealed record CreateUserRequest(string Email, string FullName, string Password);

public sealed record UpdateUserRequest(string FullName, bool IsActive);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SetWorkingContextRequest(Guid? BrandId, Guid? FiscalYearId);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record UserDetailsResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    Guid? CurrentBrandId,
    Guid? CurrentFiscalYearId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

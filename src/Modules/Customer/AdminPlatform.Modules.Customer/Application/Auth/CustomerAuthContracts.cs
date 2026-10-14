namespace AdminPlatform.Modules.Customer.Application.Auth;

public sealed record RegisterCustomerRequest(string FullName, string Phone, string Email, string Password);

/// <summary>Step 1 of registration: the sign-up details; a one-time code is sent to <see cref="Phone"/>.</summary>
public sealed record RequestRegistrationOtpRequest(string FullName, string Phone, string Email, string Password);

public sealed record RegistrationOtpResponse(DateTime ExpiresAtUtc, DateTime ResendAvailableAtUtc);

/// <summary>Step 2 of registration: the code the customer received; creates the account on success.</summary>
public sealed record VerifyRegistrationOtpRequest(string Phone, string Code, string? DeviceInfo);

public sealed record CustomerLoginRequest(string Email, string Password, string? DeviceInfo);

public sealed record GoogleLoginRequest(string IdToken, string? DeviceInfo);

public sealed record CustomerRefreshTokenRequest(string RefreshToken);

public sealed record CustomerTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record CustomerMeResponse(
    Guid Id,
    string CustomerCode,
    string FullName,
    string? Phone,
    string? Email,
    string? AvatarMediaId,
    string Status);

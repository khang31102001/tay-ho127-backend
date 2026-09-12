namespace AdminPlatform.Modules.Customer.Application.Auth;

public sealed record RegisterCustomerRequest(string FullName, string Phone, string Email, string Password);

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

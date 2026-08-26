namespace AdminPlatform.Modules.Identity.Application.Auth;

public sealed record LoginRequest(string Email, string Password, string? DeviceInfo);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record MeResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid? CurrentBrandId,
    Guid? CurrentFiscalYearId);

public sealed record SessionResponse(
    Guid Id,
    string? DeviceInfo,
    string? IpAddress,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsRevoked);

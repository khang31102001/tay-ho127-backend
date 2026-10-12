namespace AdminPlatform.Modules.Customer.Application.Auth;

/// <summary>OtpChallengeId / OtpCode: the proof that the phone belongs to the person registering — required (400 without
/// them) while "Customer:PhoneVerification:Required" is on, ignored while it is off. Get them from
/// POST /api/v1/customer/auth/register/otp.</summary>
public sealed record RegisterCustomerRequest(
    string FullName,
    string Phone,
    string Email,
    string Password,
    Guid? OtpChallengeId = null,
    string? OtpCode = null);

/// <summary>Step 1 of registering with phone verification: the details that decide where the code goes and whether the
/// account could be created at all.</summary>
public sealed record RequestRegistrationOtpRequest(string Phone, string Email);

/// <param name="ChallengeId">Send back with the code in the register request.</param>
/// <param name="ResendAfterSeconds">How long the user must wait before asking for another code.</param>
public sealed record RegistrationOtpResponse(Guid ChallengeId, DateTime ExpiresAtUtc, int ResendAfterSeconds);

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
    string Status,
    bool PhoneVerified);

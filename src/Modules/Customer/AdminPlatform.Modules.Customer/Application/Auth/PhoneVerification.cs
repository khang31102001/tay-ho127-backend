namespace AdminPlatform.Modules.Customer.Application.Auth;

/// <summary>Section "Customer:PhoneVerification". Off by default: until it is switched on the register endpoint behaves
/// exactly as before. Switch it on (CUSTOMER_PHONE_VERIFICATION_REQUIRED=true) only once a message provider is connected —
/// otherwise nobody could finish registering.</summary>
public sealed class PhoneVerificationOptions
{
    public const string SectionName = "Customer:PhoneVerification";

    public bool Required { get; set; }
}

public enum PhoneVerificationOutcome
{
    Verified,

    /// <summary>Wrong code, or a challenge that is unknown, already used or for another phone.</summary>
    Invalid,

    Expired,
    TooManyAttempts,
}

/// <param name="ChallengeId">What the client must send back with the code.</param>
public sealed record PhoneVerificationIssued(Guid ChallengeId, DateTime ExpiresAtUtc, int ResendAfterSeconds);

/// <summary>Sends a one-time code to a phone and checks it. Customer defines this port; the Host implements it with the
/// Notification module, so Customer never references that module. Phones are passed already normalised.</summary>
public interface ICustomerPhoneVerifier
{
    /// <summary>Throws TooManyRequestsException when a sending limit is hit and ServiceUnavailableException when the
    /// message could not be handed to the provider.</summary>
    Task<PhoneVerificationIssued> SendRegistrationCodeAsync(string phone, string? requestIp, CancellationToken cancellationToken);

    Task<PhoneVerificationOutcome> VerifyRegistrationCodeAsync(Guid challengeId, string phone, string code, CancellationToken cancellationToken);
}

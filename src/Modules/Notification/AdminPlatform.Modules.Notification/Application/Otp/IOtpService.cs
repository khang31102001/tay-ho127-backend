namespace AdminPlatform.Modules.Notification.Application.Otp;

/// <summary>What a code may be issued for. A code issued for one purpose never verifies another.</summary>
public static class OtpPurposes
{
    /// <summary>Proving the phone number typed on the sign-up form belongs to the person signing up.</summary>
    public const string RegisterPhone = "register-phone";

    /// <summary>The message kind the sender is asked to deliver for a purpose; null for an unknown purpose.</summary>
    internal static string? MessageKindFor(string purpose) => purpose switch
    {
        RegisterPhone => "otp-register",
        _ => null,
    };
}

/// <param name="Purpose">One of <see cref="OtpPurposes"/>.</param>
/// <param name="Phone">Already normalised ("0901234567").</param>
/// <param name="RequestIp">The caller's address for the per-address limit; null when unknown.</param>
public sealed record OtpRequest(string Purpose, string Phone, string? RequestIp);

/// <param name="ChallengeId">What the caller must present, with the code, to verify.</param>
/// <param name="ResendAfterSeconds">How long the user must wait before asking for another code.</param>
public sealed record OtpIssued(Guid ChallengeId, DateTime ExpiresAtUtc, int ResendAfterSeconds);

public enum OtpVerifyOutcome
{
    Verified,

    /// <summary>Wrong code, an unknown or already-used challenge, or a phone/purpose that does not match. The
    /// reasons are deliberately not told apart, so a probe learns nothing.</summary>
    Invalid,

    Expired,

    /// <summary>Too many wrong guesses: this challenge is finished and a new code must be requested.</summary>
    TooManyAttempts,
}

public interface IOtpService
{
    /// <summary>Issues and sends a code. Throws TooManyRequestsException when a limit is hit, BusinessRuleValidationException
    /// for a bad phone or purpose, and ServiceUnavailableException when the provider would not take the message.</summary>
    Task<OtpIssued> RequestAsync(OtpRequest request, CancellationToken cancellationToken);

    /// <summary>Checks a code. A right code is used up; every guess, right or wrong, counts as an attempt.</summary>
    Task<OtpVerifyOutcome> VerifyAsync(Guid challengeId, string purpose, string phone, string code, CancellationToken cancellationToken);
}

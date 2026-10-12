using AdminPlatform.Modules.Customer.Application.Auth;
using AdminPlatform.Modules.Notification.Application.Otp;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Customer's ICustomerPhoneVerifier port with the Notification module's one-time codes. Lives in the
/// Host because only the Host may reference both modules (architecture assumption #6).</summary>
internal sealed class CustomerPhoneVerifierAdapter : ICustomerPhoneVerifier
{
    private readonly IOtpService _otp;

    public CustomerPhoneVerifierAdapter(IOtpService otp)
    {
        _otp = otp;
    }

    public async Task<PhoneVerificationIssued> SendRegistrationCodeAsync(string phone, string? requestIp, CancellationToken cancellationToken)
    {
        var issued = await _otp.RequestAsync(new OtpRequest(OtpPurposes.RegisterPhone, phone, requestIp), cancellationToken);
        return new PhoneVerificationIssued(issued.ChallengeId, issued.ExpiresAtUtc, issued.ResendAfterSeconds);
    }

    public async Task<PhoneVerificationOutcome> VerifyRegistrationCodeAsync(
        Guid challengeId, string phone, string code, CancellationToken cancellationToken)
    {
        var outcome = await _otp.VerifyAsync(challengeId, OtpPurposes.RegisterPhone, phone, code, cancellationToken);
        return outcome switch
        {
            OtpVerifyOutcome.Verified => PhoneVerificationOutcome.Verified,
            OtpVerifyOutcome.Expired => PhoneVerificationOutcome.Expired,
            OtpVerifyOutcome.TooManyAttempts => PhoneVerificationOutcome.TooManyAttempts,
            _ => PhoneVerificationOutcome.Invalid,
        };
    }
}

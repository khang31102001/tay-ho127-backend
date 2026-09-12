using AdminPlatform.Modules.Customer.Application;
using AdminPlatform.SharedKernel;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Customer.Infrastructure;

/// <summary>Validates a Google ID token's signature, issuer, audience, and expiry against Google's published
/// certificates via the official Google.Apis.Auth library — the frontend's claimed Google profile is never
/// trusted directly (§: "Không trust dữ liệu Google từ Frontend nếu Backend chưa verify").</summary>
internal sealed class GoogleIdTokenVerifier : IGoogleIdTokenVerifier
{
    private readonly GoogleAuthOptions _options;

    public GoogleIdTokenVerifier(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleIdentityPayload> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException(
                "GoogleAuth:ClientId is not configured. Set it via an environment variable or user-secrets before enabling Google Sign-In.");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId],
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleIdentityPayload(payload.Subject, payload.Email, payload.EmailVerified, payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            throw new AuthenticationFailedException($"Invalid Google id token: {ex.Message}");
        }
    }
}

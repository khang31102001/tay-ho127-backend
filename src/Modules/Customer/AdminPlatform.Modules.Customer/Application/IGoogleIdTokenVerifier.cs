namespace AdminPlatform.Modules.Customer.Application;

public sealed record GoogleIdentityPayload(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>Server-side verification of a Google ID token (signature, issuer, audience, expiry against
/// Google's published certs) — the frontend must never be trusted to hand us a bare email/profile (§: "Không
/// trust dữ liệu Google từ Frontend nếu Backend chưa verify").</summary>
public interface IGoogleIdTokenVerifier
{
    Task<GoogleIdentityPayload> VerifyAsync(string idToken, CancellationToken cancellationToken);
}

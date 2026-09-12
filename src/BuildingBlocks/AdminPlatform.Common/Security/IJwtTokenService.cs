using System.Security.Claims;

namespace AdminPlatform.Common.Security;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(IEnumerable<Claim> claims);

    /// <summary>Cryptographically random opaque refresh token (the raw, one-time value returned to the
    /// client). Only its SHA-256 hash should ever be persisted by the caller.</summary>
    string GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}

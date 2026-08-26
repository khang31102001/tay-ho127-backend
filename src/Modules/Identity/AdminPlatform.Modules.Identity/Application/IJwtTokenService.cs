using System.Security.Claims;

namespace AdminPlatform.Modules.Identity.Application;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(IEnumerable<Claim> claims);

    /// <summary>Cryptographically random opaque refresh token (the raw, one-time value returned to the
    /// client). Only its SHA-256 hash is ever persisted — see RefreshToken.TokenHash.</summary>
    string GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}

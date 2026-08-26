namespace AdminPlatform.Common.Security;

/// <summary>Bound from the "Jwt" configuration section. SigningKey comes from an environment variable /
/// user-secrets in every environment — never hardcoded, never committed (security.md).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

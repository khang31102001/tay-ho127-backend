using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Identity.Domain;

/// <summary>One login session / device. Stores only a SHA-256 hash of the refresh token, never the raw
/// value (security.md, constraint: "phải hash, rotate và hỗ trợ revoke").</summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    private RefreshToken()
    {
        // EF Core
    }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime nowUtc, TimeSpan lifetime, string? deviceInfo, string? ipAddress)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guard.NotEmpty(userId, nameof(userId)),
            TokenHash = Guard.NotNullOrWhiteSpace(tokenHash, nameof(tokenHash)),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IssuedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(lifetime),
        };
    }

    /// <summary>Marks this token used/replaced as part of rotation. If it was already revoked, the caller
    /// is presenting a reused refresh token — treat as a reuse-detection signal.</summary>
    public void Revoke(DateTime nowUtc, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc ??= nowUtc;
        ReplacedByTokenId ??= replacedByTokenId;
    }
}

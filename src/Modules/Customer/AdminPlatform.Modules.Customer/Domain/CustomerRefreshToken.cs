using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

/// <summary>Customer's own session/device table — mirrors AdminPlatform.Modules.Identity.Domain.RefreshToken
/// exactly (same rotation/reuse-detection design) but kept as a separate table in the customer schema:
/// Customer and Admin sessions must never share storage (§7 security boundary), and modules cannot
/// project-reference each other's Domain types.</summary>
public sealed class CustomerRefreshToken : Entity
{
    public Guid CustomerId { get; private set; }
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

    private CustomerRefreshToken()
    {
        // EF Core
    }

    public static CustomerRefreshToken Issue(Guid customerId, string tokenHash, DateTime nowUtc, TimeSpan lifetime, string? deviceInfo, string? ipAddress)
    {
        return new CustomerRefreshToken
        {
            Id = Guid.NewGuid(),
            CustomerId = Guard.NotEmpty(customerId, nameof(customerId)),
            TokenHash = Guard.NotNullOrWhiteSpace(tokenHash, nameof(tokenHash)),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IssuedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(lifetime),
        };
    }

    public void Revoke(DateTime nowUtc, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc ??= nowUtc;
        ReplacedByTokenId ??= replacedByTokenId;
    }
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Notification.Domain;

/// <summary>One one-time code sent to a phone number to prove its owner holds it. The code itself is never stored,
/// only a keyed hash of it. A plain Entity (not auditable): these rows are short-lived security records, not
/// business data, and are counted to enforce the per-phone / per-address / daily sending limits.</summary>
public sealed class OtpChallenge : Entity
{
    public const int MaxPurposeLength = 64;
    public const int MaxPhoneLength = 32;
    public const int MaxCodeHashLength = 128;
    public const int MaxRequestIpLength = 64;

    /// <summary>What the code is for, e.g. "register-phone". A code issued for one purpose never verifies another.</summary>
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>The phone number in its one stored form ("0901234567").</summary>
    public string Phone { get; private set; } = string.Empty;

    public string CodeHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public string? RequestIp { get; private set; }

    /// <summary>Mapped to Postgres `xmin`: two simultaneous guesses cannot both be recorded as one attempt, which
    /// would let parallel requests get more tries than the limit allows.</summary>
    public uint RowVersion { get; private set; }

    public bool IsConsumed => ConsumedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsOutOfAttempts(int maxAttempts) => AttemptCount >= maxAttempts;

    private OtpChallenge()
    {
        // EF Core
    }

    /// <param name="id">Chosen by the caller because the code hash is bound to it, so it must exist before the hash does.</param>
    public static OtpChallenge Issue(Guid id, string purpose, string phone, string codeHash, DateTime nowUtc, TimeSpan lifetime, string? requestIp)
    {
        return new OtpChallenge
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Purpose = Guard.NotNullOrWhiteSpace(purpose, nameof(purpose)).Trim(),
            Phone = Guard.NotNullOrWhiteSpace(phone, nameof(phone)).Trim(),
            CodeHash = Guard.NotNullOrWhiteSpace(codeHash, nameof(codeHash)),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(lifetime),
            RequestIp = string.IsNullOrWhiteSpace(requestIp) ? null : requestIp.Trim(),
        };
    }

    /// <summary>Counts one guess. Every guess is counted, right or wrong, before it is compared.</summary>
    public void RegisterAttempt() => AttemptCount++;

    /// <summary>Uses the code up (or withdraws it): it can never verify again.</summary>
    public void Consume(DateTime nowUtc) => ConsumedAtUtc ??= nowUtc;
}

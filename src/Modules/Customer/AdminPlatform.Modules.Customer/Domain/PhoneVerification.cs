using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

/// <summary>A registration waiting for its phone number to be proven: the one-time code (hashed — never the
/// code itself) plus the sign-up details the customer typed. The Customer row is created only when the code is
/// confirmed, so nobody can claim a phone number they do not own. One row per phone number; asking for a new
/// code reuses it, which is also what the resend limits count against.</summary>
public sealed class PhoneVerification : Entity
{
    public string Phone { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    /// <summary>Already hashed with the password hasher: the plain password is never stored.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public string CodeHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }

    public DateTime? LastSentAtUtc { get; private set; }
    public DateTime SendWindowStartedAtUtc { get; private set; }
    public int SendsInWindow { get; private set; }

    private PhoneVerification()
    {
        // EF Core
    }

    public static PhoneVerification Start(string phone, string fullName, string email, string passwordHash, string codeHash, DateTime nowUtc, TimeSpan lifetime)
    {
        var verification = new PhoneVerification
        {
            Id = Guid.NewGuid(),
            Phone = Guard.NotNullOrWhiteSpace(phone, nameof(phone)),
            SendWindowStartedAtUtc = nowUtc,
        };
        verification.IssueCode(fullName, email, passwordHash, codeHash, nowUtc, lifetime);
        return verification;
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Seconds the customer still has to wait before another code may be sent; 0 when allowed.</summary>
    public int ResendWaitSeconds(DateTime nowUtc, TimeSpan cooldown)
    {
        if (LastSentAtUtc is not { } lastSent)
        {
            return 0;
        }

        var remaining = lastSent + cooldown - nowUtc;
        return remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
    }

    /// <summary>True when the allowance of codes for this number in the current window is used up (the window restarts by itself).</summary>
    public bool HasReachedSendLimit(DateTime nowUtc, TimeSpan window, int maxSends) =>
        nowUtc - SendWindowStartedAtUtc < window && SendsInWindow >= maxSends;

    /// <summary>Replaces the pending sign-up and its code with a fresh one (a "resend", or a retry after the code expired).</summary>
    public void Resend(string fullName, string email, string passwordHash, string codeHash, DateTime nowUtc, TimeSpan lifetime, TimeSpan window)
    {
        if (nowUtc - SendWindowStartedAtUtc >= window)
        {
            SendWindowStartedAtUtc = nowUtc;
            SendsInWindow = 0;
        }

        IssueCode(fullName, email, passwordHash, codeHash, nowUtc, lifetime);
    }

    /// <summary>The provider could not deliver the code: give the attempt back so the customer can retry at once.</summary>
    public void MarkSendFailed()
    {
        LastSentAtUtc = null;
        SendsInWindow = Math.Max(0, SendsInWindow - 1);
    }

    public void RegisterFailedAttempt() => FailedAttempts++;

    /// <summary>Kills the current code. The row stays (not deleted) so the resend limits keep counting: deleting
    /// it after too many wrong guesses would hand out a fresh allowance of codes.</summary>
    public void Expire(DateTime nowUtc) => ExpiresAtUtc = nowUtc;

    public bool HasReachedAttemptLimit(int maxAttempts) => FailedAttempts >= maxAttempts;

    private void IssueCode(string fullName, string email, string passwordHash, string codeHash, DateTime nowUtc, TimeSpan lifetime)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim();
        Email = Guard.NotNullOrWhiteSpace(email, nameof(email)).Trim().ToLowerInvariant();
        PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        CodeHash = Guard.NotNullOrWhiteSpace(codeHash, nameof(codeHash));
        ExpiresAtUtc = nowUtc.Add(lifetime);
        FailedAttempts = 0;
        LastSentAtUtc = nowUtc;
        SendsInWindow++;
    }
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

/// <summary>One login credential/identity a Customer can authenticate with — Local (email+password) or
/// Google. A Customer has at most one identity per provider (enforced by a unique index on
/// (customer_id, provider) — see CustomerAuthIdentityConfiguration). Kept as its own table rather than
/// columns on Customer itself, since Customer is a profile/business entity while auth credentials are a
/// distinct concern (security.md: "treat authentication ... as separate concerns") and a Customer can carry
/// more than one identity (account linking, §5).</summary>
public sealed class CustomerAuthIdentity : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public CustomerAuthProvider Provider { get; private set; }

    /// <summary>The provider's own subject id (Google's `sub` claim). Null for Local — a local identity is
    /// keyed by (Provider, CustomerId) instead.</summary>
    public string? ProviderUserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    /// <summary>Local provider only — a salted PBKDF2 hash (see AdminPlatform.Common.Security.IPasswordHasher).
    /// Never populated for Google.</summary>
    public string? PasswordHash { get; private set; }

    private CustomerAuthIdentity()
    {
        // EF Core
    }

    public static CustomerAuthIdentity CreateLocal(Guid customerId, string email, string passwordHash)
    {
        return new CustomerAuthIdentity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guard.NotEmpty(customerId, nameof(customerId)),
            Provider = CustomerAuthProvider.Local,
            Email = Guard.NotNullOrWhiteSpace(email, nameof(email)).Trim().ToLowerInvariant(),
            PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash)),
        };
    }

    public static CustomerAuthIdentity CreateGoogle(Guid customerId, string googleUserId, string email)
    {
        return new CustomerAuthIdentity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guard.NotEmpty(customerId, nameof(customerId)),
            Provider = CustomerAuthProvider.Google,
            ProviderUserId = Guard.NotNullOrWhiteSpace(googleUserId, nameof(googleUserId)),
            Email = Guard.NotNullOrWhiteSpace(email, nameof(email)).Trim().ToLowerInvariant(),
        };
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash));
    }
}

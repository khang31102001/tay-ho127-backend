using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

public sealed class Customer : AuditableEntity
{
    public string CustomerCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Nullable, unlike the original schema's `not null` — a Customer created via Google Sign-In
    /// (§3 Method 2) has no phone number at signup time. See the Customer/Auth report for the schema
    /// rationale. A Vietnamese number is stored in its one canonical form ("0901234567").</summary>
    public string? Phone { get; private set; }

    /// <summary>When the owner of <see cref="Phone"/> proved they hold it (by a one-time code). Null for a number
    /// nobody has proved. It belongs to ONE number: changing the phone clears it.</summary>
    public DateTime? PhoneVerifiedAtUtc { get; private set; }

    public string? Email { get; private set; }

    /// <summary>Plain id/URL of the avatar's media asset. No FK: this codebase has no Media module yet, so
    /// there is nothing to reference — see the Customer/Auth report.</summary>
    public string? AvatarMediaId { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }
    public CustomerGender? Gender { get; private set; }
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    public bool IsPhoneVerified => PhoneVerifiedAtUtc is not null;

    private Customer()
    {
        // EF Core
    }

    /// <param name="phoneVerifiedAtUtc">Pass the moment a one-time code was accepted for this very phone; leave null otherwise.</param>
    public static Customer Create(string fullName, string? phone, string? email, DateTime? phoneVerifiedAtUtc = null)
    {
        var normalizedPhone = NormalizePhone(phone);
        return new Customer
        {
            Id = Guid.NewGuid(),
            CustomerCode = GenerateCode(),
            FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim(),
            Phone = normalizedPhone,
            PhoneVerifiedAtUtc = normalizedPhone is null ? null : phoneVerifiedAtUtc,
            Email = NormalizeEmail(email),
            Status = CustomerStatus.Active,
        };
    }

    public void UpdateProfile(string fullName, string? phone, string? email, DateOnly? dateOfBirth, CustomerGender? gender)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim();

        var normalizedPhone = NormalizePhone(phone);
        if (normalizedPhone != Phone)
        {
            // A proof of ownership is for one number only; a different number has not been proved by anyone.
            PhoneVerifiedAtUtc = null;
        }

        Phone = normalizedPhone;
        Email = NormalizeEmail(email);
        DateOfBirth = dateOfBirth;
        Gender = gender;
    }

    public void UpdateAvatar(string? avatarMediaId)
    {
        AvatarMediaId = string.IsNullOrWhiteSpace(avatarMediaId) ? null : avatarMediaId.Trim();
    }

    /// <summary>Records that the owner of the CURRENT phone proved it. Nothing to prove when there is no phone.</summary>
    public void MarkPhoneVerified(DateTime nowUtc)
    {
        if (Phone is not null)
        {
            PhoneVerifiedAtUtc = nowUtc;
        }
    }

    public void Activate() => Status = CustomerStatus.Active;

    public void Deactivate() => Status = CustomerStatus.Inactive;

    private static string GenerateCode() => "KH" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static string? NormalizePhone(string? phone) => VietnamesePhone.NormalizeLenient(phone);

    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

public sealed class Customer : AuditableEntity
{
    public string CustomerCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Nullable, unlike the original schema's `not null` — a Customer created via Google Sign-In
    /// (§3 Method 2) has no phone number at signup time. See the Customer/Auth report for the schema
    /// rationale.</summary>
    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    /// <summary>Plain id/URL of the avatar's media asset. No FK: this codebase has no Media module yet, so
    /// there is nothing to reference — see the Customer/Auth report.</summary>
    public string? AvatarMediaId { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }
    public CustomerGender? Gender { get; private set; }
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    private Customer()
    {
        // EF Core
    }

    public static Customer Create(string fullName, string? phone, string? email)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            CustomerCode = GenerateCode(),
            FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim(),
            Phone = NormalizePhone(phone),
            Email = NormalizeEmail(email),
            Status = CustomerStatus.Active,
        };
    }

    public void UpdateProfile(string fullName, string? phone, string? email, DateOnly? dateOfBirth, CustomerGender? gender)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim();
        Phone = NormalizePhone(phone);
        Email = NormalizeEmail(email);
        DateOfBirth = dateOfBirth;
        Gender = gender;
    }

    public void UpdateAvatar(string? avatarMediaId)
    {
        AvatarMediaId = string.IsNullOrWhiteSpace(avatarMediaId) ? null : avatarMediaId.Trim();
    }

    public void Activate() => Status = CustomerStatus.Active;

    public void Deactivate() => Status = CustomerStatus.Inactive;

    private static string GenerateCode() => "KH" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static string? NormalizePhone(string? phone) => string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Identity.Domain;

public sealed class User : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>Working-context selection (Organization module owns Brand/FiscalYear themselves; only the
    /// selected id is stored here, cross-module by id only — see architecture assumption #6).</summary>
    public Guid? CurrentBrandId { get; private set; }
    public Guid? CurrentFiscalYearId { get; private set; }

    private User()
    {
        // EF Core
    }

    public static User Create(string email, string passwordHash, string fullName)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = Guard.NotNullOrWhiteSpace(email, nameof(email)).Trim().ToLowerInvariant(),
            PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash)),
            FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim(),
            IsActive = true,
        };
    }

    public void UpdateProfile(string fullName)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(fullName)).Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash));
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetWorkingContext(Guid? brandId, Guid? fiscalYearId)
    {
        CurrentBrandId = brandId;
        CurrentFiscalYearId = fiscalYearId;
    }
}

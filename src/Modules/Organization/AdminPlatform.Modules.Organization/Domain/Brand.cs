using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

/// <summary>How a customer reaches ONE branch: phone, address, opening hours. Everything here is per-branch — the
/// brand-wide identity (name, logo, social links...) lives in <see cref="BrandProfile"/>. Open/close are "HH:mm" and
/// come as a pair (a branch either states its hours or does not).</summary>
public sealed record BrandContact(
    string? Phone,
    string? Hotline,
    string? Email,
    string? AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string? OpenTime,
    string? CloseTime,
    string? BusinessHoursNote);

/// <summary>A BRANCH (chi nhánh): one place the business operates. It is the unit users are scoped to and the JWT carries
/// as the current working branch, and it owns its own contact details and opening hours. The code name "Brand" is
/// historical (API routes, permissions, the brand_id claim) — the business meaning is "branch". Exactly one active branch
/// can be the PRIMARY one: the address/phone/hours the public website shows.</summary>
public sealed partial class Brand : CatalogEntity
{
    public const int MaxTextLength = 500;

    public Guid OrganizationId { get; private set; }
    public string? Phone { get; private set; }
    public string? Hotline { get; private set; }
    public string? Email { get; private set; }
    public string? AddressLine { get; private set; }
    public string? Ward { get; private set; }
    public string? District { get; private set; }
    public string? Province { get; private set; }
    public string? OpenTime { get; private set; }
    public string? CloseTime { get; private set; }
    public string? BusinessHoursNote { get; private set; }

    /// <summary>The branch the public website presents. The service keeps it unique across all branches.</summary>
    public bool IsPrimary { get; private set; }

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeRegex();

    private Brand()
    {
        // EF Core
    }

    public static Brand Create(Guid organizationId, string code, string name)
    {
        return new Brand
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId)),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;

        // An inactive branch cannot represent the business on the website.
        if (!isActive)
        {
            IsPrimary = false;
        }
    }

    public void UpdateContact(BrandContact contact)
    {
        var open = Clean(contact.OpenTime);
        var close = Clean(contact.CloseTime);
        if ((open is null) != (close is null))
        {
            throw new BusinessRuleValidationException("Opening and closing time must both be set or both be empty.");
        }

        if (open is not null && (!TimeRegex().IsMatch(open) || !TimeRegex().IsMatch(close!)))
        {
            throw new BusinessRuleValidationException("Opening hours must be in HH:mm format.");
        }

        var email = Clean(contact.Email);
        if (email is not null && !email.Contains('@'))
        {
            throw new BusinessRuleValidationException("The email address is not valid.");
        }

        Phone = Clean(contact.Phone);
        Hotline = Clean(contact.Hotline);
        Email = email;
        AddressLine = Clean(contact.AddressLine);
        Ward = Clean(contact.Ward);
        District = Clean(contact.District);
        Province = Clean(contact.Province);
        OpenTime = open;
        CloseTime = close;
        BusinessHoursNote = Clean(contact.BusinessHoursNote);
    }

    public void SetPrimary(bool isPrimary)
    {
        if (isPrimary && !IsActive)
        {
            throw new BusinessRuleValidationException("An inactive branch cannot be the primary branch.");
        }

        IsPrimary = isPrimary;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

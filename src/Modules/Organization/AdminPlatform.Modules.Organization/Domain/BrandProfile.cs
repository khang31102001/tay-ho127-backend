using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

/// <summary>One social/web link of the brand.</summary>
public sealed record SocialLink(string Platform, string Url, int DisplayOrder, bool IsActive);

/// <summary>The brand-wide IDENTITY shown on the public website and in search results (name, tagline, description, logos,
/// legal name/tax code, social links) — one row for the whole business (singleton). Anything that differs per place —
/// address, phone, opening hours — belongs to a branch (<see cref="Brand"/>), not here; the website combines this profile
/// with the primary branch. Logos/images are opaque Media ids (Organization never reads the Media module).</summary>
public sealed class BrandProfile : AuditableEntity
{
    public const int MaxSocialLinks = 20;
    public const int MaxTextLength = 2000;

    public static readonly IReadOnlyList<string> AllowedPlatforms = ["website", "facebook", "instagram", "tiktok", "shopee", "zalo", "youtube"];

    public string Name { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? TaxCode { get; private set; }
    public string? LegalName { get; private set; }
    public IReadOnlyList<SocialLink> SocialLinks { get; private set; } = [];
    public string? LogoMediaId { get; private set; }
    public string? LogoDarkMediaId { get; private set; }
    public string? LogoLightMediaId { get; private set; }
    public string? FaviconMediaId { get; private set; }
    public string? OgImageMediaId { get; private set; }

    private BrandProfile()
    {
        // EF Core
    }

    public static BrandProfile Create(BrandProfileDetails details)
    {
        var profile = new BrandProfile { Id = Guid.NewGuid() };
        profile.Update(details);
        return profile;
    }

    public void Update(BrandProfileDetails details)
    {
        if (details.SocialLinks.Count > MaxSocialLinks)
        {
            throw new BusinessRuleValidationException($"A brand can have at most {MaxSocialLinks} social links.");
        }

        var links = new List<SocialLink>(details.SocialLinks.Count);
        foreach (var link in details.SocialLinks)
        {
            var platform = (link.Platform ?? string.Empty).Trim().ToLowerInvariant();
            var url = (link.Url ?? string.Empty).Trim();
            if (!AllowedPlatforms.Contains(platform))
            {
                throw new BusinessRuleValidationException($"'{link.Platform}' is not a supported platform.");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                throw new BusinessRuleValidationException($"'{link.Url}' is not a valid http(s) link.");
            }

            links.Add(new SocialLink(platform, url, link.DisplayOrder, link.IsActive));
        }

        Name = Guard.NotNullOrWhiteSpace(details.Name, nameof(details.Name)).Trim();
        Tagline = (details.Tagline ?? string.Empty).Trim();
        Description = Clean(details.Description);
        TaxCode = Clean(details.TaxCode);
        LegalName = Clean(details.LegalName);
        SocialLinks = links.OrderBy(l => l.DisplayOrder).ToList();
        LogoMediaId = Clean(details.LogoMediaId);
        LogoDarkMediaId = Clean(details.LogoDarkMediaId);
        LogoLightMediaId = Clean(details.LogoLightMediaId);
        FaviconMediaId = Clean(details.FaviconMediaId);
        OgImageMediaId = Clean(details.OgImageMediaId);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record BrandProfileDetails(
    string Name,
    string Tagline,
    string? Description,
    string? TaxCode,
    string? LegalName,
    IReadOnlyList<SocialLink> SocialLinks,
    string? LogoMediaId,
    string? LogoDarkMediaId,
    string? LogoLightMediaId,
    string? FaviconMediaId,
    string? OgImageMediaId);

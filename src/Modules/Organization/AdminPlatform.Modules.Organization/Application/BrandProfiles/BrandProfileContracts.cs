using AdminPlatform.Modules.Organization.Application.Brands;

namespace AdminPlatform.Modules.Organization.Application.BrandProfiles;

/// <summary>Platform: "website" | "facebook" | "instagram" | "tiktok" | "shopee" | "zalo" | "youtube". Url: an absolute
/// http(s) link.</summary>
public sealed record SocialLinkDto(string Platform, string Url, int DisplayOrder, bool IsActive);

/// <summary>The *MediaId fields are opaque Media ids (null = none).</summary>
public sealed record UpdateBrandProfileRequest(
    string Name,
    string Tagline,
    string? Description,
    string? TaxCode,
    string? LegalName,
    IReadOnlyList<SocialLinkDto> SocialLinks,
    string? LogoMediaId,
    string? LogoDarkMediaId,
    string? LogoLightMediaId,
    string? FaviconMediaId,
    string? OgImageMediaId);

public sealed record BrandProfileResponse(
    string Name,
    string Tagline,
    string? Description,
    string? TaxCode,
    string? LegalName,
    IReadOnlyList<SocialLinkDto> SocialLinks,
    string? LogoMediaId,
    string? LogoDarkMediaId,
    string? LogoLightMediaId,
    string? FaviconMediaId,
    string? OgImageMediaId,
    DateTime UpdatedAt);

/// <summary>The branch the website presents (contact, address, hours).</summary>
public sealed record PublicPrimaryBranchResponse(string Code, string Name, BrandContactDto Contact);

/// <summary>What the public website needs: the brand identity plus its primary branch (null when none is set up yet).</summary>
public sealed record PublicBrandResponse(BrandProfileResponse Profile, PublicPrimaryBranchResponse? PrimaryBranch);

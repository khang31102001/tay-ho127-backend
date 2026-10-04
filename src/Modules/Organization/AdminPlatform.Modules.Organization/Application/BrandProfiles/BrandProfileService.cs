using AdminPlatform.Modules.Organization.Application.Brands;
using AdminPlatform.Modules.Organization.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application.BrandProfiles;

public interface IBrandProfileService
{
    /// <summary>The single brand profile; created with the defaults the first time anything asks.</summary>
    Task<BrandProfileResponse> GetAsync(CancellationToken cancellationToken);

    Task<BrandProfileResponse> UpdateAsync(UpdateBrandProfileRequest request, CancellationToken cancellationToken);

    /// <summary>Brand identity + primary branch for the public website.</summary>
    Task<PublicBrandResponse> GetPublicAsync(CancellationToken cancellationToken);
}

public sealed class UpdateBrandProfileRequestValidator : AbstractValidator<UpdateBrandProfileRequest>
{
    public UpdateBrandProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tagline).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(Domain.BrandProfile.MaxTextLength);
        RuleFor(x => x.TaxCode).MaximumLength(50);
        RuleFor(x => x.LegalName).MaximumLength(300);
        RuleFor(x => x.SocialLinks).NotNull().Must(links => links.Count <= Domain.BrandProfile.MaxSocialLinks)
            .WithMessage($"A brand can have at most {Domain.BrandProfile.MaxSocialLinks} social links.");
        RuleForEach(x => x.SocialLinks).ChildRules(link =>
        {
            link.RuleFor(l => l.Platform).NotEmpty().MaximumLength(32);
            link.RuleFor(l => l.Url).NotEmpty().MaximumLength(500);
        });
    }
}

public sealed class BrandProfileService : IBrandProfileService
{
    private static readonly BrandProfileDetails Defaults = new(
        "Bánh Cuốn Tây Hồ 127", "Bánh cuốn truyền thống, phục vụ nhanh, hương vị gia đình Bắc giữa Sài Gòn.",
        null, null, null, [], null, null, null, null, null);

    private readonly IOrganizationDbContext _db;

    public BrandProfileService(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<BrandProfileResponse> GetAsync(CancellationToken cancellationToken) =>
        ToResponse(await GetOrCreateAsync(cancellationToken));

    public async Task<BrandProfileResponse> UpdateAsync(UpdateBrandProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await GetOrCreateAsync(cancellationToken);
        profile.Update(new BrandProfileDetails(
            request.Name,
            request.Tagline,
            request.Description,
            request.TaxCode,
            request.LegalName,
            request.SocialLinks.Select(l => new SocialLink(l.Platform, l.Url, l.DisplayOrder, l.IsActive)).ToList(),
            request.LogoMediaId,
            request.LogoDarkMediaId,
            request.LogoLightMediaId,
            request.FaviconMediaId,
            request.OgImageMediaId));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(profile);
    }

    public async Task<PublicBrandResponse> GetPublicAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.BrandProfiles.AsNoTracking().OrderBy(p => p.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var response = ToResponse(profile ?? Domain.BrandProfile.Create(Defaults));

        var branch = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.IsPrimary && b.IsActive, cancellationToken);
        return new PublicBrandResponse(
            response,
            branch is null ? null : new PublicPrimaryBranchResponse(branch.Code, branch.Name, BrandService.ToContact(branch)));
    }

    private async Task<Domain.BrandProfile> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.BrandProfiles.OrderBy(p => p.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = Domain.BrandProfile.Create(Defaults);
        _db.BrandProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static BrandProfileResponse ToResponse(Domain.BrandProfile p) => new(
        p.Name, p.Tagline, p.Description, p.TaxCode, p.LegalName,
        p.SocialLinks.Select(l => new SocialLinkDto(l.Platform, l.Url, l.DisplayOrder, l.IsActive)).ToList(),
        p.LogoMediaId, p.LogoDarkMediaId, p.LogoLightMediaId, p.FaviconMediaId, p.OgImageMediaId,
        p.UpdatedAtUtc ?? p.CreatedAtUtc);
}

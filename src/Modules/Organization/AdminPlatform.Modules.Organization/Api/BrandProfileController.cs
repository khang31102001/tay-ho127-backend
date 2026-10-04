using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.BrandProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

/// <summary>The brand-wide identity (name, tagline, logos, legal info, social links) — one record for the whole business.
/// Per-place details (address, phone, hours) belong to branches: see /api/v1/brands.</summary>
[ApiController]
[Route("api/v1/organization/brand-profile")]
public sealed class BrandProfileController : ControllerBase
{
    private readonly IBrandProfileService _service;

    public BrandProfileController(IBrandProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.BrandProfileView)]
    [ProducesResponseType<BrandProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandProfileResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _service.GetAsync(cancellationToken));

    /// <summary>400 for an unsupported platform or a link that is not http(s).</summary>
    [HttpPut]
    [RequirePermission(OrganizationPermissions.BrandProfileUpdate)]
    [ProducesResponseType<BrandProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandProfileResponse>> Update([FromBody] UpdateBrandProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(request, cancellationToken));
}

/// <summary>Anonymous read for the website: brand identity + the primary branch contact, address and hours.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/organization/public")]
public sealed class PublicBrandController : ControllerBase
{
    private readonly IBrandProfileService _service;

    public PublicBrandController(IBrandProfileService service)
    {
        _service = service;
    }

    [HttpGet("brand")]
    [ProducesResponseType<PublicBrandResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicBrandResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _service.GetPublicAsync(cancellationToken));
}

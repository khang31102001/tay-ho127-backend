using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Seo.Application.Settings;
using AdminPlatform.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Seo.Api;

/// <summary>The site-wide SEO settings (a singleton — there is no id and no create/delete).</summary>
[ApiController]
[Route("api/v1/seo/settings")]
public sealed class SeoSettingsController : ControllerBase
{
    private readonly ISeoSettingsService _seoSettingsService;

    public SeoSettingsController(ISeoSettingsService seoSettingsService)
    {
        _seoSettingsService = seoSettingsService;
    }

    /// <summary>404 only when the seed has never run.</summary>
    [HttpGet]
    [RequirePermission(SeoPermissions.SeoSettingsView)]
    [ProducesResponseType<SeoSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _seoSettingsService.GetAsync(cancellationToken) ?? throw new NotFoundException("SeoSettings", "singleton"));
    }

    /// <summary>400 when the title template lacks "%s" or a robots path is not a site path.</summary>
    [HttpPut]
    [RequirePermission(SeoPermissions.SeoSettingsUpdate)]
    [ProducesResponseType<SeoSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSettingsResponse>> Update([FromBody] UpdateSeoSettingsRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _seoSettingsService.UpdateAsync(request, cancellationToken));
    }
}

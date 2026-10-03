using AdminPlatform.Modules.Seo.Application.Settings;
using AdminPlatform.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Seo.Api;

/// <summary>Anonymous read-only SEO data for the public website (robots.txt, meta fallbacks).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/seo/public")]
public sealed class PublicSeoController : ControllerBase
{
    private readonly ISeoSettingsService _seoSettingsService;

    public PublicSeoController(ISeoSettingsService seoSettingsService)
    {
        _seoSettingsService = seoSettingsService;
    }

    /// <summary>404 only when the seed has never run — the website then falls back to built-in defaults.</summary>
    [HttpGet("settings")]
    [ProducesResponseType<SeoSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        return Ok(await _seoSettingsService.GetAsync(cancellationToken) ?? throw new NotFoundException("SeoSettings", "singleton"));
    }
}

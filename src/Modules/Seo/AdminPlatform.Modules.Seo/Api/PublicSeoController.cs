using AdminPlatform.Modules.Seo.Application.Metadata;
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
    private readonly ISeoMetadataService _seoMetadataService;

    public PublicSeoController(ISeoSettingsService seoSettingsService, ISeoMetadataService seoMetadataService)
    {
        _seoSettingsService = seoSettingsService;
        _seoMetadataService = seoMetadataService;
    }

    /// <summary>404 only when the seed has never run — the website then falls back to built-in defaults.</summary>
    [HttpGet("settings")]
    [ProducesResponseType<SeoSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        return Ok(await _seoSettingsService.GetAsync(cancellationToken) ?? throw new NotFoundException("SeoSettings", "singleton"));
    }

    /// <summary>The override of one entity (EntityId is omitted for "homepage"); 404 when it has none — the website then
    /// uses the entity's own data and the site-wide settings.</summary>
    [HttpGet("metadata")]
    [ProducesResponseType<SeoMetadataResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoMetadataResponse>> GetMetadata(
        [FromQuery] string entityType, [FromQuery] string? entityId, CancellationToken cancellationToken)
    {
        return Ok(await _seoMetadataService.FindAsync(entityType, entityId, cancellationToken)
            ?? throw new NotFoundException("SeoMetadata", $"{entityType}:{entityId}"));
    }

    /// <summary>Entities set to "do not index" — the website leaves them out of the sitemap.</summary>
    [HttpGet("noindex")]
    [ProducesResponseType<IReadOnlyList<NoIndexEntityResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NoIndexEntityResponse>>> ListNoIndex(CancellationToken cancellationToken)
    {
        return Ok(await _seoMetadataService.ListNoIndexAsync(cancellationToken));
    }
}

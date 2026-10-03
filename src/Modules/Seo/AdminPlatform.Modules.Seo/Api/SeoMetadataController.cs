using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Seo.Application.Metadata;
using AdminPlatform.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Seo.Api;

/// <summary>Per-entity SEO overrides. An override is addressed by (entityType, entityId) — homepage has no id —
/// not by its own id, and is saved with a single upsert.</summary>
[ApiController]
[Route("api/v1/seo/metadata")]
public sealed class SeoMetadataController : ControllerBase
{
    private readonly ISeoMetadataService _seoMetadataService;

    public SeoMetadataController(ISeoMetadataService seoMetadataService)
    {
        _seoMetadataService = seoMetadataService;
    }

    [HttpGet]
    [RequirePermission(SeoPermissions.SeoMetadataView)]
    [ProducesResponseType<PagedResult<SeoMetadataResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SeoMetadataResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? entityType, CancellationToken cancellationToken)
    {
        return Ok(await _seoMetadataService.ListAsync(request, entityType, cancellationToken));
    }

    /// <summary>404 when the entity has no override.</summary>
    [HttpGet("lookup")]
    [RequirePermission(SeoPermissions.SeoMetadataView)]
    [ProducesResponseType<SeoMetadataResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoMetadataResponse>> Lookup(
        [FromQuery] string entityType, [FromQuery] string? entityId, CancellationToken cancellationToken)
    {
        return Ok(await _seoMetadataService.FindAsync(entityType, entityId, cancellationToken)
            ?? throw new NotFoundException("SeoMetadata", $"{entityType}:{entityId}"));
    }

    /// <summary>Creates or replaces the entity's override. 400 for an unknown entity type, a missing entity id
    /// (except homepage) or a canonical URL that is not a site path / http(s) URL.</summary>
    [HttpPut]
    [RequirePermission(SeoPermissions.SeoMetadataUpdate)]
    [ProducesResponseType<SeoMetadataResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoMetadataResponse>> Upsert([FromBody] UpsertSeoMetadataRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _seoMetadataService.UpsertAsync(request, cancellationToken));
    }

    /// <summary>Resets the entity to its defaults; 204 even when it had no override.</summary>
    [HttpDelete]
    [RequirePermission(SeoPermissions.SeoMetadataDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset([FromQuery] string entityType, [FromQuery] string? entityId, CancellationToken cancellationToken)
    {
        await _seoMetadataService.ResetAsync(entityType, entityId, cancellationToken);
        return NoContent();
    }
}

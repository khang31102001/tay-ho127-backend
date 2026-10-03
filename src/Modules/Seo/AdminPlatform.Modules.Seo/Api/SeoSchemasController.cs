using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Seo.Application.Schemas;
using AdminPlatform.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Seo.Api;

/// <summary>Schema.org / JSON-LD overrides, one per (entityType, entityId, schemaType) — homepage has no id.
/// Saved with a single upsert; edited inside each entity's SEO editor, so there is no list endpoint.</summary>
[ApiController]
[Route("api/v1/seo/schemas")]
public sealed class SeoSchemasController : ControllerBase
{
    private readonly ISeoSchemaService _seoSchemaService;

    public SeoSchemasController(ISeoSchemaService seoSchemaService)
    {
        _seoSchemaService = seoSchemaService;
    }

    /// <summary>404 when the entity has no row for that schema type.</summary>
    [HttpGet("lookup")]
    [RequirePermission(SeoPermissions.SeoSchemasView)]
    [ProducesResponseType<SeoSchemaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSchemaResponse>> Lookup(
        [FromQuery] string entityType, [FromQuery] string? entityId, [FromQuery] string schemaType, CancellationToken cancellationToken)
    {
        return Ok(await _seoSchemaService.FindAsync(entityType, entityId, schemaType, cancellationToken)
            ?? throw new NotFoundException("SeoSchema", $"{entityType}:{entityId}:{schemaType}"));
    }

    /// <summary>Creates or replaces the row. 400 for an unknown entity/schema type, a missing entity id (except homepage),
    /// invalid JSON-LD, or Advanced Mode without a JSON-LD.</summary>
    [HttpPut]
    [RequirePermission(SeoPermissions.SeoSchemasUpdate)]
    [ProducesResponseType<SeoSchemaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SeoSchemaResponse>> Upsert([FromBody] UpsertSeoSchemaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _seoSchemaService.UpsertAsync(request, cancellationToken));
    }

    /// <summary>Back to the generated schema; 204 even when there was no row.</summary>
    [HttpDelete]
    [RequirePermission(SeoPermissions.SeoSchemasDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(
        [FromQuery] string entityType, [FromQuery] string? entityId, [FromQuery] string schemaType, CancellationToken cancellationToken)
    {
        await _seoSchemaService.ResetAsync(entityType, entityId, schemaType, cancellationToken);
        return NoContent();
    }
}

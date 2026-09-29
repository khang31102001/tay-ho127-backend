using AdminPlatform.Modules.Catalog.Application.PublicCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/public")]
public sealed class PublicCatalogController : ControllerBase
{
    private readonly IPublicCatalogService _publicCatalogService;

    public PublicCatalogController(IPublicCatalogService publicCatalogService)
    {
        _publicCatalogService = publicCatalogService;
    }

    /// <summary>Anonymous snapshot of the active catalog for the public website: active categories,
    /// products and menus, available product placements, and modifier groups. No admin-only fields.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PublicCatalogResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicCatalogResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _publicCatalogService.GetAsync(cancellationToken));
    }
}

using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.Brands;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

[ApiController]
[Route("api/v1/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.BrandsView)]
    [ProducesResponseType<PagedResult<BrandResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BrandResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _brandService.ListAsync(request, organizationId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(OrganizationPermissions.BrandsView)]
    [ProducesResponseType<BrandResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _brandService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(OrganizationPermissions.BrandsCreate)]
    [ProducesResponseType<BrandResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BrandResponse>> Create([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var created = await _brandService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(OrganizationPermissions.BrandsUpdate)]
    [ProducesResponseType<BrandResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandResponse>> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _brandService.UpdateAsync(id, request, cancellationToken));
    }
}

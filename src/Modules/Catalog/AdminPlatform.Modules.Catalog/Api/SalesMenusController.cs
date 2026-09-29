using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.SalesMenus;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/sales-menus")]
public sealed class SalesMenusController : ControllerBase
{
    private readonly ISalesMenuService _salesMenuService;

    public SalesMenusController(ISalesMenuService salesMenuService)
    {
        _salesMenuService = salesMenuService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.SalesMenusView)]
    [ProducesResponseType<PagedResult<SalesMenuResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SalesMenuResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuService.ListAsync(request, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusView)]
    [ProducesResponseType<SalesMenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesMenuResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when the code is already used.</summary>
    [HttpPost]
    [RequirePermission(CatalogPermissions.SalesMenusCreate)]
    [ProducesResponseType<SalesMenuResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SalesMenuResponse>> Create([FromBody] CreateSalesMenuRequest request, CancellationToken cancellationToken)
    {
        var created = await _salesMenuService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusUpdate)]
    [ProducesResponseType<SalesMenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesMenuResponse>> Update(Guid id, [FromBody] UpdateSalesMenuRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also removes every product placement on the menu.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _salesMenuService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

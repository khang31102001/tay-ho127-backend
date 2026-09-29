using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

/// <summary>Product placements on sales menus. A flat resource (not nested under a menu) because the
/// admin screen lists placements across all menus and can move one to another menu.</summary>
[ApiController]
[Route("api/v1/catalog/sales-menu-products")]
public sealed class SalesMenuProductsController : ControllerBase
{
    private readonly ISalesMenuProductService _salesMenuProductService;

    public SalesMenuProductsController(ISalesMenuProductService salesMenuProductService)
    {
        _salesMenuProductService = salesMenuProductService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.SalesMenusView)]
    [ProducesResponseType<PagedResult<SalesMenuProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SalesMenuProductResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? salesMenuId, [FromQuery] Guid? productId, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuProductService.ListAsync(request, salesMenuId, productId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusView)]
    [ProducesResponseType<SalesMenuProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesMenuProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuProductService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when the product is already on that menu.</summary>
    [HttpPost]
    [RequirePermission(CatalogPermissions.SalesMenusCreate)]
    [ProducesResponseType<SalesMenuProductResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SalesMenuProductResponse>> Create([FromBody] CreateSalesMenuProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _salesMenuProductService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusUpdate)]
    [ProducesResponseType<SalesMenuProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesMenuProductResponse>> Update(Guid id, [FromBody] UpdateSalesMenuProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _salesMenuProductService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.SalesMenusDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _salesMenuProductService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

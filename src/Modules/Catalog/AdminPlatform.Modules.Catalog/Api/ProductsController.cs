using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.ProductsView)]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? categoryId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _productService.ListAsync(request, categoryId, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.ProductsView)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>A blank slug is generated from the name; an explicit slug already in use returns 409.</summary>
    [HttpPost]
    [RequirePermission(CatalogPermissions.ProductsCreate)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>A blank slug keeps the current one. MediaIds/ModifierGroupIds replace the current lists.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.ProductsUpdate)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also removes the product from every sales menu.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.ProductsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

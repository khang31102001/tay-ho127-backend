using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.CategoriesView)]
    [ProducesResponseType<PagedResult<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? parentId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.ListAsync(request, parentId, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.CategoriesView)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>The tree is at most 3 levels deep (group → category → sub-category); 400 otherwise.</summary>
    [HttpPost]
    [RequirePermission(CatalogPermissions.CategoriesCreate)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.CategoriesUpdate)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>409 while the category still has sub-categories or products.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.CategoriesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

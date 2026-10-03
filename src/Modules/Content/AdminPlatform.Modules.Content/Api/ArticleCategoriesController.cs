using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.ArticleCategories;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

[ApiController]
[Route("api/v1/content/article-categories")]
public sealed class ArticleCategoriesController : ControllerBase
{
    private readonly IArticleCategoryService _categoryService;

    public ArticleCategoriesController(IArticleCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [RequirePermission(ContentPermissions.ArticleCategoriesView)]
    [ProducesResponseType<PagedResult<ArticleCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ArticleCategoryResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? parentId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.ListAsync(request, parentId, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleCategoriesView)]
    [ProducesResponseType<ArticleCategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleCategoryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>400 when the tree would exceed 3 levels; 409 when an explicit slug is already used.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.ArticleCategoriesCreate)]
    [ProducesResponseType<ArticleCategoryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleCategoryResponse>> Create(
        [FromBody] CreateArticleCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleCategoriesUpdate)]
    [ProducesResponseType<ArticleCategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleCategoryResponse>> Update(
        Guid id, [FromBody] UpdateArticleCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>409 while the category still has sub-categories or articles.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleCategoriesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

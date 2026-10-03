using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.Articles;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

[ApiController]
[Route("api/v1/content/articles")]
public sealed class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    /// <summary>The list omits the HTML body; fetch an article by id for it.</summary>
    [HttpGet]
    [RequirePermission(ContentPermissions.ArticlesView)]
    [ProducesResponseType<PagedResult<ArticleListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ArticleListItemResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, [FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        return Ok(await _articleService.ListAsync(request, status, categoryId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticlesView)]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _articleService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when an explicit slug is already used. The HTML content is sanitized before it is stored.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.ArticlesCreate)]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleResponse>> Create([FromBody] CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var created = await _articleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticlesUpdate)]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleResponse>> Update(Guid id, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _articleService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticlesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _articleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

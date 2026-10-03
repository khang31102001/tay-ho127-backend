using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.ArticleTags;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

[ApiController]
[Route("api/v1/content/article-tags")]
public sealed class ArticleTagsController : ControllerBase
{
    private readonly IArticleTagService _tagService;

    public ArticleTagsController(IArticleTagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [RequirePermission(ContentPermissions.ArticleTagsView)]
    [ProducesResponseType<PagedResult<ArticleTagResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ArticleTagResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tagService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleTagsView)]
    [ProducesResponseType<ArticleTagResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleTagResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _tagService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when an explicit slug is already used.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.ArticleTagsCreate)]
    [ProducesResponseType<ArticleTagResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleTagResponse>> Create([FromBody] CreateArticleTagRequest request, CancellationToken cancellationToken)
    {
        var created = await _tagService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleTagsUpdate)]
    [ProducesResponseType<ArticleTagResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleTagResponse>> Update(Guid id, [FromBody] UpdateArticleTagRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tagService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also removes the tag from every article that carried it.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.ArticleTagsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

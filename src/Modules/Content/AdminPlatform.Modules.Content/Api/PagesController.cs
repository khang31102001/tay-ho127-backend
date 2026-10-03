using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.Pages;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

[ApiController]
[Route("api/v1/content/pages")]
public sealed class PagesController : ControllerBase
{
    private readonly IPageService _pageService;

    public PagesController(IPageService pageService)
    {
        _pageService = pageService;
    }

    [HttpGet]
    [RequirePermission(ContentPermissions.PagesView)]
    [ProducesResponseType<PagedResult<PageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PageResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await _pageService.ListAsync(request, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesView)]
    [ProducesResponseType<PageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _pageService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when an explicit path is already used; 400 when the path is not "/" or lowercase /segments.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.PagesCreate)]
    [ProducesResponseType<PageResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PageResponse>> Create([FromBody] CreatePageRequest request, CancellationToken cancellationToken)
    {
        var created = await _pageService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesUpdate)]
    [ProducesResponseType<PageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse>> Update(Guid id, [FromBody] UpdatePageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _pageService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also removes every section of the page.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _pageService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

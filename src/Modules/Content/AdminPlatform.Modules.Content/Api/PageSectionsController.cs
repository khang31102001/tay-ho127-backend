using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.PageSections;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

/// <summary>Sections of a page. They use the pages.* permissions, like sales-menu products use sales-menus.*.</summary>
[ApiController]
[Route("api/v1/content/pages/{pageId:guid}/sections")]
public sealed class PageSectionsController : ControllerBase
{
    private readonly IPageSectionService _sectionService;

    public PageSectionsController(IPageSectionService sectionService)
    {
        _sectionService = sectionService;
    }

    /// <summary>All sections of the page by display order. 404 when the page does not exist.</summary>
    [HttpGet]
    [RequirePermission(ContentPermissions.PagesView)]
    [ProducesResponseType<IReadOnlyList<PageSectionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PageSectionResponse>>> List(Guid pageId, CancellationToken cancellationToken)
    {
        return Ok(await _sectionService.ListAsync(pageId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesView)]
    [ProducesResponseType<PageSectionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageSectionResponse>> GetById(Guid pageId, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sectionService.GetByIdAsync(pageId, id, cancellationToken));
    }

    /// <summary>400 when the CTA URL is not a site path / http(s) URL.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.PagesCreate)]
    [ProducesResponseType<PageSectionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PageSectionResponse>> Create(
        Guid pageId, [FromBody] CreatePageSectionRequest request, CancellationToken cancellationToken)
    {
        var created = await _sectionService.CreateAsync(pageId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { pageId, id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesUpdate)]
    [ProducesResponseType<PageSectionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageSectionResponse>> Update(
        Guid pageId, Guid id, [FromBody] UpdatePageSectionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sectionService.UpdateAsync(pageId, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.PagesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid pageId, Guid id, CancellationToken cancellationToken)
    {
        await _sectionService.DeleteAsync(pageId, id, cancellationToken);
        return NoContent();
    }
}

using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Navigation.Application.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Navigation.Api;

/// <summary>Items of the admin sidebar and of the website menus. The permission (`menus.*` for the admin sidebar,
/// `site-navigation.*` for the website) depends on the scope of the item's menu, so it is checked in the service.</summary>
[ApiController]
[Route("api/v1/navigation/items")]
[Authorize]
public sealed class NavigationItemsController : ControllerBase
{
    private readonly INavigationItemService _itemService;

    public NavigationItemsController(INavigationItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<NavigationItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NavigationItemResponse>>> List(
        [FromQuery] Guid? menuId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _itemService.ListAsync(menuId, request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<NavigationItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NavigationItemResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _itemService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<NavigationItemResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NavigationItemResponse>> Create(
        [FromBody] CreateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        var created = await _itemService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<NavigationItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NavigationItemResponse>> Update(
        Guid id, [FromBody] UpdateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _itemService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _itemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Drag-and-drop / move up-down: puts the given items, in order, under one parent and renumbers them.</summary>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reorder([FromBody] ReorderNavigationItemsRequest request, CancellationToken cancellationToken)
    {
        await _itemService.ReorderAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _itemService.GetPermissionCodesAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPermissions(
        Guid id, [FromBody] AssignNavigationItemPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _itemService.SetPermissionsAsync(id, request, cancellationToken);
        return NoContent();
    }
}

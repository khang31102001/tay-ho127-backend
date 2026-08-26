using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Navigation.Application.Menus;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Navigation.Api;

[ApiController]
[Route("api/v1/menus")]
public sealed class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    [RequirePermission(NavigationPermissions.MenusView)]
    [ProducesResponseType<PagedResult<MenuResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MenuResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.ListAsync(request, cancellationToken));
    }

    [HttpGet("tree")]
    [RequirePermission(NavigationPermissions.MenusView)]
    [ProducesResponseType<IReadOnlyList<MenuTreeNode>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuTreeNode>>> GetTree(CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetTreeAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(NavigationPermissions.MenusView)]
    [ProducesResponseType<MenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MenuResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(NavigationPermissions.MenusCreate)]
    [ProducesResponseType<MenuResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MenuResponse>> Create([FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var created = await _menuService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(NavigationPermissions.MenusUpdate)]
    [ProducesResponseType<MenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MenuResponse>> Update(Guid id, [FromBody] UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(NavigationPermissions.MenusDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _menuService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    [RequirePermission(NavigationPermissions.MenusView)]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetPermissionCodesAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(NavigationPermissions.MenusManagePermissions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] AssignMenuPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _menuService.SetPermissionsAsync(id, request, cancellationToken);
        return NoContent();
    }
}

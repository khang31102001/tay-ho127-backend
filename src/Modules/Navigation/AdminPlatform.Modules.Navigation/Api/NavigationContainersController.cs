using AdminPlatform.Modules.Navigation.Application.Containers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Navigation.Api;

/// <summary>The navigation containers (admin sidebar + website header/footer/mobile). Which permission applies
/// (`menus.*` or `site-navigation.*`) depends on the container's scope, so it is checked in the service.</summary>
[ApiController]
[Route("api/v1/navigation/containers")]
[Authorize]
public sealed class NavigationContainersController : ControllerBase
{
    private readonly INavigationMenuService _menuService;

    public NavigationContainersController(INavigationMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NavigationMenuResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NavigationMenuResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _menuService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<NavigationMenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NavigationMenuResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<NavigationMenuResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NavigationMenuResponse>> Update(
        Guid id, [FromBody] UpdateNavigationMenuRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.UpdateAsync(id, request, cancellationToken));
    }
}

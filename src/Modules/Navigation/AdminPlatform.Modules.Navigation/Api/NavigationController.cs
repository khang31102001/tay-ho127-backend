using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Navigation.Application.MyNavigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Navigation.Api;

[ApiController]
[Route("api/v1/navigation")]
[Authorize]
public sealed class NavigationController : ControllerBase
{
    private readonly IMyNavigationService _myNavigationService;
    private readonly ICurrentUser _currentUser;

    public NavigationController(IMyNavigationService myNavigationService, ICurrentUser currentUser)
    {
        _myNavigationService = myNavigationService;
        _currentUser = currentUser;
    }

    /// <summary>The admin sidebar tree for the caller, pre-filtered by their own permissions — the frontend renders
    /// this directly without needing to know any permission codes itself. `menus` is the original path, kept so an
    /// older frontend keeps working during a staggered deploy.</summary>
    [HttpGet("me")]
    [HttpGet("menus")]
    [ProducesResponseType<IReadOnlyList<MenuTreeNode>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuTreeNode>>> GetMySidebar(CancellationToken cancellationToken)
    {
        return Ok(await _myNavigationService.GetVisibleMenuTreeAsync(_currentUser.Permissions, cancellationToken));
    }
}

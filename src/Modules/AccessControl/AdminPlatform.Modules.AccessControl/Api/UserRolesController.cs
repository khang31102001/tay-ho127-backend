using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application.UserRoles;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.AccessControl.Api;

[ApiController]
[Route("api/v1/users/{userId:guid}/roles")]
public sealed class UserRolesController : ControllerBase
{
    private readonly IUserRoleService _userRoleService;

    public UserRolesController(IUserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }

    [HttpGet]
    [RequirePermission(AccessControlPermissions.RolesView)]
    [ProducesResponseType<IReadOnlyList<UserRoleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserRoleResponse>>> List(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _userRoleService.ListForUserAsync(userId, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(AccessControlPermissions.UsersManageRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await _userRoleService.AssignAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roleId:guid}")]
    [RequirePermission(AccessControlPermissions.UsersManageRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await _userRoleService.RemoveAsync(userId, roleId, cancellationToken);
        return NoContent();
    }
}

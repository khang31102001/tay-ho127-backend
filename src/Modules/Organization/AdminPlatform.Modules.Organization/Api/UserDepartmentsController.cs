using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.UserScopes;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

[ApiController]
[Route("api/v1/users/{userId:guid}/departments")]
public sealed class UserDepartmentsController : ControllerBase
{
    private readonly IUserScopeService _userScopeService;

    public UserDepartmentsController(IUserScopeService userScopeService)
    {
        _userScopeService = userScopeService;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.DepartmentsView)]
    [ProducesResponseType<IReadOnlyList<UserDepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDepartmentResponse>>> List(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _userScopeService.ListDepartmentsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(OrganizationPermissions.UsersManageDepartments)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignDepartmentRequest request, CancellationToken cancellationToken)
    {
        await _userScopeService.AssignDepartmentAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{departmentId:guid}")]
    [RequirePermission(OrganizationPermissions.UsersManageDepartments)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid userId, Guid departmentId, CancellationToken cancellationToken)
    {
        await _userScopeService.RemoveDepartmentAsync(userId, departmentId, cancellationToken);
        return NoContent();
    }
}

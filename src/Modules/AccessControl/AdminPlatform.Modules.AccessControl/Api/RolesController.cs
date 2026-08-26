using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.AccessControl.Api;

[ApiController]
[Route("api/v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequirePermission(AccessControlPermissions.RolesView)]
    [ProducesResponseType<PagedResult<RoleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoleResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(AccessControlPermissions.RolesView)]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(AccessControlPermissions.RolesCreate)]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var created = await _roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(AccessControlPermissions.RolesUpdate)]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleResponse>> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(AccessControlPermissions.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    [RequirePermission(AccessControlPermissions.RolesView)]
    [ProducesResponseType<IReadOnlyList<Guid>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetPermissionIdsAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(AccessControlPermissions.RolesManagePermissions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _roleService.SetPermissionsAsync(id, request, cancellationToken);
        return NoContent();
    }
}

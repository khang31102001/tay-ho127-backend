using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.AccessControl.Api;

[ApiController]
[Route("api/v1/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [RequirePermission(AccessControlPermissions.PermissionsView)]
    [ProducesResponseType<PagedResult<PermissionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PermissionResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _permissionService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(AccessControlPermissions.PermissionsView)]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _permissionService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(AccessControlPermissions.PermissionsCreate)]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PermissionResponse>> Create([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var created = await _permissionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(AccessControlPermissions.PermissionsUpdate)]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionResponse>> Update(Guid id, [FromBody] UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _permissionService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(AccessControlPermissions.PermissionsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _permissionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

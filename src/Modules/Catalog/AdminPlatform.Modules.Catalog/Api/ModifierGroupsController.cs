using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.ModifierGroups;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/modifier-groups")]
public sealed class ModifierGroupsController : ControllerBase
{
    private readonly IModifierGroupService _modifierGroupService;

    public ModifierGroupsController(IModifierGroupService modifierGroupService)
    {
        _modifierGroupService = modifierGroupService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.ModifierGroupsView)]
    [ProducesResponseType<PagedResult<ModifierGroupResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ModifierGroupResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _modifierGroupService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.ModifierGroupsView)]
    [ProducesResponseType<ModifierGroupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModifierGroupResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _modifierGroupService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(CatalogPermissions.ModifierGroupsCreate)]
    [ProducesResponseType<ModifierGroupResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ModifierGroupResponse>> Create([FromBody] CreateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        var created = await _modifierGroupService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Options are replaced as a whole list; options sent with their id keep that id.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.ModifierGroupsUpdate)]
    [ProducesResponseType<ModifierGroupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModifierGroupResponse>> Update(Guid id, [FromBody] UpdateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _modifierGroupService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also detaches the group from every product.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.ModifierGroupsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _modifierGroupService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

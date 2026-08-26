using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Platform.Application.SystemSettings;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Platform.Api;

[ApiController]
[Route("api/v1/system-settings")]
public sealed class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _systemSettingService;

    public SystemSettingsController(ISystemSettingService systemSettingService)
    {
        _systemSettingService = systemSettingService;
    }

    [HttpGet]
    [RequirePermission(PlatformPermissions.SystemSettingsView)]
    [ProducesResponseType<PagedResult<SystemSettingResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SystemSettingResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _systemSettingService.ListAsync(request, organizationId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PlatformPermissions.SystemSettingsView)]
    [ProducesResponseType<SystemSettingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _systemSettingService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(PlatformPermissions.SystemSettingsCreate)]
    [ProducesResponseType<SystemSettingResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SystemSettingResponse>> Create([FromBody] CreateSystemSettingRequest request, CancellationToken cancellationToken)
    {
        var created = await _systemSettingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PlatformPermissions.SystemSettingsUpdate)]
    [ProducesResponseType<SystemSettingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingResponse>> Update(Guid id, [FromBody] UpdateSystemSettingRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _systemSettingService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PlatformPermissions.SystemSettingsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _systemSettingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

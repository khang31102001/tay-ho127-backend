using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Platform.Application.AuditLogs;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Platform.Api;

[ApiController]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public AuditLogsController(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    [HttpGet]
    [RequirePermission(PlatformPermissions.AuditLogsView)]
    [ProducesResponseType<PagedResult<AuditLogResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> Search(
        [FromQuery] PagedRequest request, [FromQuery] AuditLogQuery filter, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogQueryService.SearchAsync(request, filter, cancellationToken));
    }
}

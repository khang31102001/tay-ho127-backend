using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Platform.Application.AuditLogs;

public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogResponse>> SearchAsync(PagedRequest request, AuditLogQuery filter, CancellationToken cancellationToken);
}

using AdminPlatform.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Platform.Application.AuditLogs;

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IPlatformDbContext _db;

    public AuditLogQueryService(IPlatformDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogResponse>> SearchAsync(PagedRequest request, AuditLogQuery filter, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.ActorUserId is { } actorId)
        {
            query = query.Where(a => a.ActorUserId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(a => a.EntityName == filter.EntityName);
        }

        if (filter.FromDate is { } fromDate)
        {
            var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.AtUtc >= fromUtc);
        }

        if (filter.ToDate is { } toDate)
        {
            var toUtc = toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.AtUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(a => EF.Functions.ILike(a.Action, pattern) || EF.Functions.ILike(a.EntityName, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(a => a.AtUtc) : query.OrderBy(a => a.AtUtc);
        if (!request.IsDescending && request.SortBy is null)
        {
            // Default to newest-first unless the caller explicitly asked for ascending.
            query = query.OrderByDescending(a => a.AtUtc);
        }

        var projected = query.Select(a => new AuditLogResponse(
            a.Id, a.ActorUserId, a.Action, a.EntityName, a.EntityId, a.ChangesJson, a.AtUtc, a.CorrelationId));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }
}

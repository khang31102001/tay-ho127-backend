using AdminPlatform.Common.Auditing;
using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Domain;

namespace AdminPlatform.Modules.Platform.Infrastructure;

/// <summary>Implements Common's IAuditEventSink port — every other module's DbContext forwards its
/// AuditableEntity changes here (via DI, no project reference to this module) after its own SaveChanges
/// commits. This is a secondary, best-effort write into the Platform module's own AuditLogs table, not
/// part of the originating module's transaction — acceptable for an admin audit trail.</summary>
internal sealed class PlatformAuditEventSink : IAuditEventSink
{
    private readonly IPlatformDbContext _db;

    public PlatformAuditEventSink(IPlatformDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(IReadOnlyCollection<AuditEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        foreach (var e in events)
        {
            _db.AuditLogs.Add(AuditLog.Create(
                e.ActorUserId, e.Action.ToString(), e.EntityName, e.EntityId, e.ChangesJson, e.AtUtc, e.CorrelationId));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

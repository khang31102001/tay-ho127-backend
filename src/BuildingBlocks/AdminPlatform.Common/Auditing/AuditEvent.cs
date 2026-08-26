namespace AdminPlatform.Common.Auditing;

public enum AuditAction
{
    Created,
    Updated,
    Deleted
}

/// <summary>One captured change to an auditable entity, forwarded to <see cref="IAuditEventSink"/>
/// after the owning module's SaveChanges succeeds.</summary>
public sealed record AuditEvent(
    Guid? ActorUserId,
    AuditAction Action,
    string EntityName,
    string EntityId,
    string? ChangesJson,
    DateTime AtUtc,
    string? CorrelationId);

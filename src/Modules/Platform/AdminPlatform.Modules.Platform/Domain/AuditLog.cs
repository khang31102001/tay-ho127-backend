using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Platform.Domain;

/// <summary>Append-only audit trail entry. Deliberately not an AuditableEntity — a log record doesn't get
/// "updated" or need its own audit trail. Populated by AdminPlatform.Common.Persistence.AuditLogSinkInterceptor
/// (via IAuditEventSink) whenever any module's DbContext saves an AuditableEntity change.</summary>
public sealed class AuditLog : Entity
{
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? ChangesJson { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    private AuditLog()
    {
        // EF Core
    }

    public static AuditLog Create(
        Guid? actorUserId, string action, string entityName, string entityId, string? changesJson, DateTime atUtc, string? correlationId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = Guard.NotNullOrWhiteSpace(action, nameof(action)),
            EntityName = Guard.NotNullOrWhiteSpace(entityName, nameof(entityName)),
            EntityId = Guard.NotNullOrWhiteSpace(entityId, nameof(entityId)),
            ChangesJson = changesJson,
            AtUtc = atUtc,
            CorrelationId = correlationId,
        };
    }
}

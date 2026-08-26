namespace AdminPlatform.Modules.Platform.Application.AuditLogs;

public sealed record AuditLogResponse(
    Guid Id, Guid? ActorUserId, string Action, string EntityName, string EntityId,
    string? ChangesJson, DateTime AtUtc, string? CorrelationId);

public sealed record AuditLogQuery(Guid? ActorUserId, string? EntityName, DateOnly? FromDate, DateOnly? ToDate);

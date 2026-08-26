namespace AdminPlatform.Common.Auditing;

/// <summary>Port implemented by the Platform module (backed by the AuditLogs table) and consumed by
/// every other module's DbContext without a project reference between them — wired at the composition root.</summary>
public interface IAuditEventSink
{
    Task RecordAsync(IReadOnlyCollection<AuditEvent> events, CancellationToken cancellationToken);
}

/// <summary>Default no-op sink so modules keep working before the Platform module registers the real one
/// (e.g. in isolated unit tests).</summary>
internal sealed class NullAuditEventSink : IAuditEventSink
{
    public Task RecordAsync(IReadOnlyCollection<AuditEvent> events, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

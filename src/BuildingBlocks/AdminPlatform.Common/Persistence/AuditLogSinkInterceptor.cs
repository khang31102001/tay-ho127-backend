using System.Text.Json;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Auditing;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AdminPlatform.Common.Persistence;

/// <summary>Captures Added/Modified/Deleted <see cref="AuditableEntity"/> changes before save and forwards
/// them to <see cref="IAuditEventSink"/> once the save has actually committed. Registered on every module's
/// DbContext; the sink itself is implemented by the Platform module and wired at the composition root, so
/// this interceptor never references Platform directly.</summary>
public sealed class AuditLogSinkInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions ChangesJsonOptions = new() { WriteIndented = false };

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IAuditEventSink _sink;

    private List<AuditEvent> _pending = [];

    public AuditLogSinkInterceptor(
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ICorrelationIdAccessor correlationIdAccessor,
        IAuditEventSink sink)
    {
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _correlationIdAccessor = correlationIdAccessor;
        _sink = sink;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _pending = Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pending.Count > 0)
        {
            var events = _pending;
            _pending = [];
            await _sink.RecordAsync(events, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEvent> Capture(DbContext? context)
    {
        if (context is null)
        {
            return [];
        }

        var now = _dateTimeProvider.UtcNow;
        var actorId = _currentUser.UserId;
        var correlationId = _correlationIdAccessor.CorrelationId;
        var events = new List<AuditEvent>();

        foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => (AuditAction?)null
            };

            if (action is null)
            {
                continue;
            }

            var changedProperties = entry.State == EntityState.Modified
                ? entry.Properties
                    .Where(p => p.IsModified)
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue)
                : null;

            events.Add(new AuditEvent(
                actorId,
                action.Value,
                entry.Entity.GetType().Name,
                entry.Entity.Id.ToString(),
                changedProperties is { Count: > 0 } ? JsonSerializer.Serialize(changedProperties, ChangesJsonOptions) : null,
                now,
                correlationId));
        }

        return events;
    }
}

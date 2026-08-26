using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Auditing;
using AdminPlatform.Common.Persistence;
using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminPlatform.Common;

public static class CommonServiceCollectionExtensions
{
    /// <summary>Registers the cross-cutting building blocks every module depends on: current-user/date-time
    /// abstractions, correlation id, the audit-log interceptors, and the dynamic permission policy provider.
    /// Call once from the Host composition root before any module's AddXModule().</summary>
    public static IServiceCollection AddPlatformCommon(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();
        services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.TryAddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();

        // Overridden by the Platform module's real AuditLogs-backed sink; keeps other modules usable without it.
        services.TryAddScoped<IAuditEventSink, NullAuditEventSink>();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<AuditLogSinkInterceptor>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}

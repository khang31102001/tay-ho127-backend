using AdminPlatform.Common.Auditing;
using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Application.AuditLogs;
using AdminPlatform.Modules.Platform.Application.FiscalYears;
using AdminPlatform.Modules.Platform.Application.SystemSettings;
using AdminPlatform.Modules.Platform.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Platform;

public static class PlatformModule
{
    /// <summary>Call this after AddPlatformCommon() so the real audit sink here overrides Common's
    /// no-op default — see AdminPlatform.Common.CommonServiceCollectionExtensions.</summary>
    public static IServiceCollection AddPlatformModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PlatformDbContext.Schema));
            // Safe to include the audit sink interceptor here too: AuditLog itself is a plain Entity, not
            // an AuditableEntity, so writing an AuditLog row never triggers another audit event.
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IPlatformDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());

        services.AddScoped<IAuditEventSink, PlatformAuditEventSink>();

        services.AddScoped<FiscalYearService>();
        services.AddScoped<IFiscalYearService>(sp => sp.GetRequiredService<FiscalYearService>());
        services.AddScoped<IFiscalYearAccessQueryService>(sp => sp.GetRequiredService<FiscalYearService>());
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();

        services.AddValidatorsFromAssembly(typeof(PlatformModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(PlatformModule).Assembly);

        return services;
    }
}

using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Integration.Application;
using AdminPlatform.Modules.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Integration;

/// <summary>Composition entry point for the Integration module: how the system connects to third-party providers
/// (non-secret settings only). The Host calls AddIntegrationModule() once, after AddPlatformCommon().</summary>
public static class IntegrationModule
{
    public static IServiceCollection AddIntegrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<IntegrationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IntegrationDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<IntegrationDbContext>());

        return services;
    }
}

using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Application.Metadata;
using AdminPlatform.Modules.Seo.Application.Settings;
using AdminPlatform.Modules.Seo.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Seo;

/// <summary>Composition entry point for the Seo module (site-wide SEO settings and per-entity metadata overrides; redirects and
/// schema follow). The Host calls AddSeoModule() once — see ContentModule for the pattern.</summary>
public static class SeoModule
{
    public static IServiceCollection AddSeoModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<SeoDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", SeoDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<ISeoDbContext>(sp => sp.GetRequiredService<SeoDbContext>());

        services.AddScoped<ISeoSettingsService, SeoSettingsService>();
        services.AddScoped<ISeoMetadataService, SeoMetadataService>();

        services.AddValidatorsFromAssembly(typeof(SeoModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(SeoModule).Assembly);

        return services;
    }
}

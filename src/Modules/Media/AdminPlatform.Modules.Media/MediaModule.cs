using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Media;

/// <summary>Composition entry point for the Media module. The Host calls AddMediaModule() once; everything
/// the module needs (DbContext, services, validators, controllers) is registered here — see
/// AdminPlatform.Modules.Platform.PlatformModule for the pattern this mirrors.</summary>
public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<MediaDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", MediaDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IMediaDbContext>(sp => sp.GetRequiredService<MediaDbContext>());

        services.AddScoped<IMediaService, MediaService>();

        services.AddValidatorsFromAssembly(typeof(MediaModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(MediaModule).Assembly);

        return services;
    }
}

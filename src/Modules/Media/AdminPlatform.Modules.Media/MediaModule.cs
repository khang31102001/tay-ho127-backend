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

        var storageSection = configuration.GetSection(MediaStorageOptions.SectionName);
        services.Configure<MediaStorageOptions>(storageSection);
        var storage = storageSection.Get<MediaStorageOptions>() ?? new MediaStorageOptions();

        // Local luôn có; Cloudinary chỉ đăng ký khi đủ khóa. MediaService chọn theo IFileStorage.Provider.
        services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
        if (storage.Cloudinary.IsConfigured)
        {
            services.AddSingleton<IFileStorage, CloudinaryFileStorage>();
        }

        if (string.Equals(storage.Provider, MediaStorageOptions.CloudinaryProvider, StringComparison.OrdinalIgnoreCase))
        {
            // Mặc định là Cloudinary mà thiếu khóa thì báo lỗi lúc khởi động, không đợi tới lần upload đầu tiên.
            services.AddOptions<MediaStorageOptions>()
                .Validate(
                    o => o.Cloudinary.IsConfigured,
                    "MediaStorage:Cloudinary:CloudName, ApiKey and ApiSecret are required when MediaStorage:Provider is Cloudinary.")
                .ValidateOnStart();
        }
        else if (!string.Equals(storage.Provider, MediaStorageOptions.LocalProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unknown MediaStorage:Provider '{storage.Provider}'. Use 'Local' or 'Cloudinary'.");
        }

        services.AddScoped<IMediaService, MediaService>();

        services.AddValidatorsFromAssembly(typeof(MediaModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(MediaModule).Assembly);

        return services;
    }
}

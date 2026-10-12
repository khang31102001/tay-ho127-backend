using AdminPlatform.Modules.Notification.Application;
using AdminPlatform.Modules.Notification.Application.Otp;
using AdminPlatform.Modules.Notification.Application.Ports;
using AdminPlatform.Modules.Notification.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminPlatform.Modules.Notification;

/// <summary>Composition entry point for the Notification module: one-time codes sent to a phone, and the log of
/// what was handed to message providers. The Host calls AddNotificationModule() once.</summary>
public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        // No audit interceptors: nothing here is an AuditableEntity — OTP rows and delivery logs are short-lived
        // operational records, not business data worth an audit trail of their own.
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
        });
        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());

        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<LoggingMessageSenderOptions>(configuration.GetSection(LoggingMessageSenderOptions.SectionName));
        services.AddScoped<IOtpService, OtpService>();

        // The default sender delivers nothing. A host that wires a real provider registers its IMessageSender first
        // and this is skipped.
        services.TryAddScoped<IMessageSender, LoggingMessageSender>();

        return services;
    }
}

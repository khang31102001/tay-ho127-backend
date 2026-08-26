using AdminPlatform.Common.Persistence;
using AdminPlatform.Common.Security;
using FluentValidation;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Auth;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Identity;

/// <summary>Composition entry point for the Identity module. The Host calls AddIdentityModule() once;
/// everything the module needs (DbContext, services, validators, controllers) is registered here.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema));
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddValidatorsFromAssembly(typeof(IdentityModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(IdentityModule).Assembly);

        return services;
    }
}

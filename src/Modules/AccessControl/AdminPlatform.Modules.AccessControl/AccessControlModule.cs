using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using AdminPlatform.Modules.AccessControl.Application.UserRoles;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.AccessControl;

public static class AccessControlModule
{
    public static IServiceCollection AddAccessControlModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<AccessControlDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AccessControlDbContext.Schema));
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IAccessControlDbContext>(sp => sp.GetRequiredService<AccessControlDbContext>());

        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IRolePermissionQueryService, RolePermissionQueryService>();

        services.AddValidatorsFromAssembly(typeof(AccessControlModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(AccessControlModule).Assembly);

        return services;
    }
}

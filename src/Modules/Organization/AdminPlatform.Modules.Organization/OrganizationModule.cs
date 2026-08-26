using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Organization.Application.Brands;
using AdminPlatform.Modules.Organization.Application.Departments;
using AdminPlatform.Modules.Organization.Application.Organizations;
using AdminPlatform.Modules.Organization.Application.UserScopes;
using AdminPlatform.Modules.Organization.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Organization;

public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", OrganizationDbContext.Schema));
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IOrganizationDbContext>(sp => sp.GetRequiredService<OrganizationDbContext>());

        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<UserScopeService>();
        services.AddScoped<IUserScopeService>(sp => sp.GetRequiredService<UserScopeService>());
        services.AddScoped<IUserScopeQueryService>(sp => sp.GetRequiredService<UserScopeService>());

        services.AddValidatorsFromAssembly(typeof(OrganizationModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(OrganizationModule).Assembly);

        return services;
    }
}

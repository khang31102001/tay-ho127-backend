using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Application.Menus;
using AdminPlatform.Modules.Navigation.Application.MyNavigation;
using AdminPlatform.Modules.Navigation.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Navigation;

public static class NavigationModule
{
    public static IServiceCollection AddNavigationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<NavigationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", NavigationDbContext.Schema));
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<INavigationDbContext>(sp => sp.GetRequiredService<NavigationDbContext>());

        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IMyNavigationService, MyNavigationService>();

        services.AddValidatorsFromAssembly(typeof(NavigationModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(NavigationModule).Assembly);

        return services;
    }
}

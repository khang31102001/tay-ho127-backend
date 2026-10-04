using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Application.Containers;
using AdminPlatform.Modules.Navigation.Application.Items;
using AdminPlatform.Modules.Navigation.Application.MyNavigation;
using AdminPlatform.Modules.Navigation.Application.Public;
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
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<INavigationDbContext>(sp => sp.GetRequiredService<NavigationDbContext>());

        services.Configure<NavigationOptions>(configuration.GetSection(NavigationOptions.SectionName));

        services.AddScoped<INavigationMenuService, NavigationMenuService>();
        services.AddScoped<INavigationItemService, NavigationItemService>();
        services.AddScoped<IMyNavigationService, MyNavigationService>();
        services.AddScoped<IPublicNavigationService, PublicNavigationService>();

        services.AddValidatorsFromAssembly(typeof(NavigationModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(NavigationModule).Assembly);

        return services;
    }
}

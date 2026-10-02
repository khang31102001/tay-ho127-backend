using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.ModifierGroups;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Application.PublicCatalog;
using AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;
using AdminPlatform.Modules.Catalog.Application.SalesMenus;
using AdminPlatform.Modules.Catalog.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Catalog;

/// <summary>Composition entry point for the Catalog module (categories, products, sales menus and their
/// products, modifier groups). The Host calls AddCatalogModule() once — see MediaModule for the pattern.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISalesMenuService, SalesMenuService>();
        services.AddScoped<ISalesMenuProductService, SalesMenuProductService>();
        services.AddScoped<IModifierGroupService, ModifierGroupService>();
        services.AddScoped<IPublicCatalogService, PublicCatalogService>();

        services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(CatalogModule).Assembly);

        return services;
    }
}

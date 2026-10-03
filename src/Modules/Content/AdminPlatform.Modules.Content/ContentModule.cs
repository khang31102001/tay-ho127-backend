using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.ArticleCategories;
using AdminPlatform.Modules.Content.Application.ArticleTags;
using AdminPlatform.Modules.Content.Application.Articles;
using AdminPlatform.Modules.Content.Application.Banners;
using AdminPlatform.Modules.Content.Application.PageSections;
using AdminPlatform.Modules.Content.Application.Pages;
using AdminPlatform.Modules.Content.Application.PublicContent;
using AdminPlatform.Modules.Content.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Content;

/// <summary>Composition entry point for the Content module (articles and their categories/tags; pages and
/// banners follow). The Host calls AddContentModule() once — see CatalogModule for the pattern.</summary>
public static class ContentModule
{
    public static IServiceCollection AddContentModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<ContentDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IContentDbContext>(sp => sp.GetRequiredService<ContentDbContext>());

        // The sanitizer holds a configured whitelist and is thread-safe, so one instance serves all requests.
        services.AddSingleton<IHtmlContentSanitizer, HtmlContentSanitizer>();

        services.AddScoped<IArticleCategoryService, ArticleCategoryService>();
        services.AddScoped<IArticleTagService, ArticleTagService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IPageSectionService, PageSectionService>();
        services.AddScoped<IPublicContentService, PublicContentService>();

        services.AddValidatorsFromAssembly(typeof(ContentModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(ContentModule).Assembly);

        return services;
    }
}

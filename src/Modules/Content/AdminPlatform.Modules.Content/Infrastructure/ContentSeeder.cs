using System.Text.Json;
using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Application.Banners;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Content.Infrastructure;

/// <summary>Initial website content (article categories/tags/articles and banners), from the embedded
/// Seed/content-seed.json exported from the frontend's former mock data. Keys in the file only link rows
/// together; real ids are new GUIDs.
///
/// Each group (articles with their taxonomy; banners) is seeded only while ITS tables are completely empty, so
/// it is safe in the every-deploy `seed` step: once a group has any data, admins own it and the seed never
/// re-creates something they deleted — and a group added later still reaches an already-seeded database.</summary>
public static class ContentSeeder
{
    private const string SeedResourceName = "AdminPlatform.Modules.Content.content-seed.json";

    /// <returns>true when at least one group was seeded.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IContentDbContext>();
        var seed = LoadSeed();

        var seededArticles = await SeedArticlesAsync(db, seed, cancellationToken);
        var seededBanners = await SeedBannersAsync(db, seed, cancellationToken);
        return seededArticles || seededBanners;
    }

    private static async Task<bool> SeedArticlesAsync(IContentDbContext db, ContentSeed seed, CancellationToken cancellationToken)
    {
        var hasArticleData = await db.ArticleCategories.AnyAsync(cancellationToken)
            || await db.ArticleTags.AnyAsync(cancellationToken)
            || await db.Articles.AnyAsync(cancellationToken);
        if (hasArticleData)
        {
            return false;
        }

        var seededAtUtc = DateTime.UtcNow;

        // Parents are listed before children in the file, so ids resolve in a single pass.
        var categoryIds = new Dictionary<string, Guid>();
        foreach (var seedCategory in seed.Categories)
        {
            Guid? parentId = seedCategory.ParentKey is null ? null : categoryIds[seedCategory.ParentKey];
            var category = ArticleCategory.Create(seedCategory.Name, seedCategory.Slug, parentId, seedCategory.SortOrder, seedCategory.IsActive);
            categoryIds[seedCategory.Key] = category.Id;
            db.ArticleCategories.Add(category);
        }

        var tagIds = new Dictionary<string, Guid>();
        foreach (var seedTag in seed.Tags)
        {
            var tag = ArticleTag.Create(seedTag.Name, seedTag.Slug);
            tagIds[seedTag.Key] = tag.Id;
            db.ArticleTags.Add(tag);
        }

        foreach (var seedArticle in seed.Articles)
        {
            var status = Enum.Parse<PublishStatus>(seedArticle.Status, ignoreCase: true);
            var details = new ArticleDetails(seedArticle.Title, seedArticle.Slug, seedArticle.Summary, seedArticle.Content,
                seedArticle.FeaturedMediaId, categoryIds[seedArticle.CategoryKey], seedArticle.AuthorName, status);

            // The seed content is trusted (it ships with the code), so it is not run through the sanitizer.
            var article = Article.Create(details, seededAtUtc);
            article.SetPublishedAt(status == PublishStatus.Published ? seedArticle.PublishedAt : null);
            article.ReplaceTags(seedArticle.TagKeys.Select(key => tagIds[key]).ToList());
            db.Articles.Add(article);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> SeedBannersAsync(IContentDbContext db, ContentSeed seed, CancellationToken cancellationToken)
    {
        if (await db.Banners.AnyAsync(cancellationToken))
        {
            return false;
        }

        foreach (var seedBanner in seed.Banners)
        {
            BannerWireFormat.TryParsePlacement(seedBanner.Placement, out var placement);
            db.Banners.Add(Banner.Create(new BannerDetails(seedBanner.Name, seedBanner.DesktopMediaId, seedBanner.MobileMediaId,
                seedBanner.AltText, seedBanner.Heading, seedBanner.Subheading, seedBanner.CtaLabel, seedBanner.CtaUrl, placement,
                seedBanner.StartAt, seedBanner.EndAt, seedBanner.DisplayOrder, seedBanner.IsActive)));
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ContentSeed LoadSeed()
    {
        using var stream = typeof(ContentSeeder).Assembly.GetManifestResourceStream(SeedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SeedResourceName}' is missing.");
        return JsonSerializer.Deserialize<ContentSeed>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Embedded resource '{SeedResourceName}' is empty.");
    }

    private sealed record ContentSeed(
        IReadOnlyList<SeedCategory> Categories,
        IReadOnlyList<SeedTag> Tags,
        IReadOnlyList<SeedArticle> Articles,
        IReadOnlyList<SeedBanner> Banners);

    private sealed record SeedCategory(string Key, string Name, string Slug, string? ParentKey, int SortOrder, bool IsActive);

    private sealed record SeedTag(string Key, string Name, string Slug);

    private sealed record SeedBanner(
        string Key,
        string Name,
        string? DesktopMediaId,
        string? MobileMediaId,
        string AltText,
        string? Heading,
        string? Subheading,
        string? CtaLabel,
        string? CtaUrl,
        string Placement,
        DateTime? StartAt,
        DateTime? EndAt,
        int DisplayOrder,
        bool IsActive);

    private sealed record SeedArticle(
        string Key,
        string Title,
        string Slug,
        string Summary,
        string Content,
        string? FeaturedMediaId,
        string CategoryKey,
        IReadOnlyList<string> TagKeys,
        string AuthorName,
        string Status,
        DateTime? PublishedAt);
}

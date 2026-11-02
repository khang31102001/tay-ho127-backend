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
        var seededPages = await SeedPagesAsync(db, seed, cancellationToken);
        var seededSlots = await SeedMissingSlotsAsync(db, seed, cancellationToken);
        return seededArticles || seededBanners || seededPages || seededSlots;
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

    private static async Task<bool> SeedPagesAsync(IContentDbContext db, ContentSeed seed, CancellationToken cancellationToken)
    {
        if (await db.Pages.AnyAsync(cancellationToken) || await db.PageSections.AnyAsync(cancellationToken))
        {
            return false;
        }

        var seededAtUtc = DateTime.UtcNow;
        var pageIds = new Dictionary<string, Guid>();
        foreach (var seedPage in seed.Pages)
        {
            var status = Enum.Parse<PublishStatus>(seedPage.Status, ignoreCase: true);
            var page = Page.Create(seedPage.Name, seedPage.Slug, status, seededAtUtc, seedPage.PageKey, seedPage.SortOrder);
            page.SetPublishedAt(status == PublishStatus.Published ? seedPage.PublishedAt : null);
            pageIds[seedPage.Key] = page.Id;
            db.Pages.Add(page);
        }

        foreach (var seedSection in seed.PageSections)
        {
            db.PageSections.Add(ToSection(pageIds[seedSection.PageKey], seedSection));
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Unlike the groups above, runs on every deploy: adds each seeded page that has keyed sections and does not
    /// exist yet (matched by slug), and each keyed seed section whose (page, key) slot is still empty — so a slot added to
    /// the website later reaches a database already in use. Never changes an existing page or section. A deleted slot
    /// section comes back on the next deploy, so the admin screen hides slots instead of deleting them.</summary>
    private static async Task<bool> SeedMissingSlotsAsync(IContentDbContext db, ContentSeed seed, CancellationToken cancellationToken)
    {
        var keyedSections = seed.PageSections.Where(s => s.SectionKey is not null).ToList();
        if (keyedSections.Count == 0)
        {
            return false;
        }

        var seededAtUtc = DateTime.UtcNow;
        var existingPages = await db.Pages.Select(p => new { p.Id, p.Slug, p.PageKey }).ToListAsync(cancellationToken);
        var pageIds = new Dictionary<string, Guid>();
        var hasChanges = false;
        foreach (var seedPage in seed.Pages.Where(p => keyedSections.Any(s => s.PageKey == p.Key)))
        {
            // The page key is the identity; the path only finds a page created before keys existed.
            var existing = existingPages.FirstOrDefault(p => seedPage.PageKey != null && p.PageKey == seedPage.PageKey)
                ?? existingPages.FirstOrDefault(p => p.Slug == seedPage.Slug);
            var pageId = existing?.Id ?? Guid.Empty;
            if (existing is null)
            {
                var status = Enum.Parse<PublishStatus>(seedPage.Status, ignoreCase: true);
                var page = Page.Create(seedPage.Name, seedPage.Slug, status, seededAtUtc, seedPage.PageKey, seedPage.SortOrder);
                page.SetPublishedAt(status == PublishStatus.Published ? seedPage.PublishedAt : null);
                db.Pages.Add(page);
                pageId = page.Id;
                hasChanges = true;
            }

            pageIds[seedPage.Key] = pageId;
        }

        var existingIds = pageIds.Values.ToList();
        var takenSlots = (await db.PageSections
                .Where(s => existingIds.Contains(s.PageId) && s.SectionKey != null)
                .Select(s => new { s.PageId, s.SectionKey })
                .ToListAsync(cancellationToken))
            .Select(s => (s.PageId, s.SectionKey!))
            .ToHashSet();

        foreach (var seedSection in keyedSections)
        {
            var pageId = pageIds[seedSection.PageKey];
            if (takenSlots.Add((pageId, seedSection.SectionKey!)))
            {
                db.PageSections.Add(ToSection(pageId, seedSection));
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return hasChanges;
    }

    private static PageSection ToSection(Guid pageId, SeedPageSection seedSection)
    {
        ContentWireFormat.TryParseSectionKind(seedSection.SectionKind, out var kind);
        var items = seedSection.Items?.Select(i => (PageSectionItem?)new PageSectionItem(i.Label, i.Title, i.Text, i.MediaId, i.Url));
        return PageSection.Create(pageId, new SectionDetails(kind, seedSection.Eyebrow, seedSection.Heading, seedSection.Subheading,
            seedSection.Body, seedSection.MediaId, seedSection.CtaLabel, seedSection.CtaUrl, seedSection.DisplayOrder, seedSection.IsVisible,
            PageSectionSettings.Create(seedSection.GalleryMediaIds, items), seedSection.SectionKey));
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
        IReadOnlyList<SeedBanner> Banners,
        IReadOnlyList<SeedPage> Pages,
        IReadOnlyList<SeedPageSection> PageSections);

    private sealed record SeedCategory(string Key, string Name, string Slug, string? ParentKey, int SortOrder, bool IsActive);

    private sealed record SeedTag(string Key, string Name, string Slug);

    /// <summary>Key only links seed rows together inside the file; PageKey is the page's real stable key.</summary>
    private sealed record SeedPage(string Key, string Name, string Slug, string Status, DateTime? PublishedAt, string? PageKey = null, int SortOrder = 0);

    private sealed record SeedPageSection(
        string Key,
        string PageKey,
        string SectionKind,
        string? Eyebrow,
        string? Heading,
        string? Subheading,
        string? Body,
        string? MediaId,
        string? CtaLabel,
        string? CtaUrl,
        int DisplayOrder,
        bool IsVisible,
        IReadOnlyList<string>? GalleryMediaIds = null,
        string? SectionKey = null,
        IReadOnlyList<SeedPageSectionItem>? Items = null);

    private sealed record SeedPageSectionItem(string? Label, string? Title, string? Text, string? MediaId, string? Url);

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

using System.Text.RegularExpressions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.PublicContent;

public sealed partial class PublicContentService : IPublicContentService
{
    private const int WordsPerMinute = 200;

    private readonly IContentDbContext _db;

    public PublicContentService(IContentDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicArticleSummaryResponse>> ListArticlesAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        // Content is read only to estimate the reading time; it is not part of the response.
        var page = await _db.Articles.AsNoTracking()
            .Where(a => a.Status == PublishStatus.Published)
            .OrderByDescending(a => a.PublishedAtUtc)
            .Select(a => new
            {
                a.Id, a.Slug, a.Title, a.Summary, a.FeaturedMediaId, a.CategoryId, a.AuthorName, a.PublishedAtUtc, a.UpdatedAtUtc, a.Content,
            })
            .ToPagedResultAsync(request, cancellationToken);

        var ids = page.Items.Select(a => a.Id).ToList();
        var tagsByArticle = (await _db.Articles.AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .SelectMany(a => a.Tags.Select(link => new { a.Id, link.TagId }))
                .ToListAsync(cancellationToken))
            .GroupBy(link => link.Id)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)group.Select(link => link.TagId).ToList());

        var items = page.Items.Select(a => new PublicArticleSummaryResponse(
            a.Id, a.Slug, a.Title, a.Summary, a.FeaturedMediaId, a.CategoryId, tagsByArticle.GetValueOrDefault(a.Id, []),
            a.AuthorName, a.PublishedAtUtc ?? DateTime.MinValue, a.UpdatedAtUtc ?? a.PublishedAtUtc ?? DateTime.MinValue,
            EstimateReadingTimeMinutes(a.Content))).ToList();
        return new PagedResult<PublicArticleSummaryResponse>(items, page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PublicArticleResponse> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.AsNoTracking()
            .Include(a => a.Tags)
            .SingleOrDefaultAsync(a => a.Slug == slug && a.Status == PublishStatus.Published, cancellationToken)
            ?? throw new NotFoundException(nameof(Article), slug);

        return new PublicArticleResponse(
            article.Id, article.Slug, article.Title, article.Summary, article.Content, article.FeaturedMediaId, article.CategoryId,
            article.Tags.Select(link => link.TagId).ToList(), article.AuthorName, article.PublishedAtUtc ?? DateTime.MinValue,
            article.UpdatedAtUtc ?? article.PublishedAtUtc ?? DateTime.MinValue, EstimateReadingTimeMinutes(article.Content));
    }

    public async Task<PublicTaxonomyResponse> GetTaxonomyAsync(CancellationToken cancellationToken)
    {
        var categories = await _db.ArticleCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new PublicArticleCategoryResponse(c.Id, c.Name, c.Slug, c.ParentId, c.SortOrder))
            .ToListAsync(cancellationToken);

        var tags = await _db.ArticleTags.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new PublicArticleTagResponse(t.Id, t.Name, t.Slug))
            .ToListAsync(cancellationToken);

        return new PublicTaxonomyResponse(categories, tags);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    /// <summary>Whole minutes to read the HTML body at ~200 words/minute, never less than 1.</summary>
    public static int EstimateReadingTimeMinutes(string html)
    {
        var text = HtmlTagRegex().Replace(html, " ");
        var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(wordCount / (double)WordsPerMinute, MidpointRounding.AwayFromZero));
    }
}

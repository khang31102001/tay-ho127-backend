using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.Articles;

public sealed class ArticleService : IArticleService
{
    private readonly IContentDbContext _db;
    private readonly IHtmlContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArticleService(IContentDbContext db, IHtmlContentSanitizer sanitizer, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _sanitizer = sanitizer;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<ArticleListItemResponse>> ListAsync(
        PagedRequest request, string? status, Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = _db.Articles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ContentWireFormat.TryParseStatus(status, out var parsedStatus))
            {
                throw new BusinessRuleValidationException("Status must be draft, published or archived.");
            }

            query = query.Where(a => a.Status == parsedStatus);
        }

        if (categoryId is { } category)
        {
            query = query.Where(a => a.CategoryId == category);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(a => EF.Functions.ILike(a.Title, pattern) || EF.Functions.ILike(a.Slug, pattern));
        }

        query = request.IsDescending ? query.OrderBy(a => a.CreatedAtUtc) : query.OrderByDescending(a => a.CreatedAtUtc);

        // Content (HTML) is deliberately not selected; tag links come from a split query so the page is
        // not multiplied by the number of tags.
        var page = await query
            .Select(a => new
            {
                a.Id, a.Title, a.Slug, a.Summary, a.FeaturedMediaId, a.CategoryId, a.AuthorName, a.Status,
                a.PublishedAtUtc, a.CreatedAtUtc, a.UpdatedAtUtc,
            })
            .ToPagedResultAsync(request, cancellationToken);

        var ids = page.Items.Select(a => a.Id).ToList();
        var tagsByArticle = (await _db.Articles.AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .SelectMany(a => a.Tags.Select(link => new { a.Id, link.TagId }))
                .ToListAsync(cancellationToken))
            .GroupBy(link => link.Id)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)group.Select(link => link.TagId).ToList());

        var items = page.Items.Select(a => new ArticleListItemResponse(
            a.Id, a.Title, a.Slug, a.Summary, a.FeaturedMediaId, a.CategoryId,
            tagsByArticle.GetValueOrDefault(a.Id, []), a.AuthorName, ContentWireFormat.ToWire(a.Status),
            a.PublishedAtUtc, a.CreatedAtUtc, a.UpdatedAtUtc)).ToList();
        return new PagedResult<ArticleListItemResponse>(items, page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<ArticleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken)
    {
        await EnsureReferencesExistAsync(request.CategoryId, request.TagIds, cancellationToken);
        var slug = await SlugResolver.ResolveAsync(request.Slug, request.Title, "article",
            _db.Articles.Select(a => a.Slug), cancellationToken);

        var article = Article.Create(ToDetails(request.Title, slug, request.Summary, request.Content, request.FeaturedMediaId,
            request.CategoryId, request.AuthorName, request.Status), _dateTimeProvider.UtcNow);
        article.ReplaceTags(request.TagIds ?? []);

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(article);
    }

    public async Task<ArticleResponse> UpdateAsync(Guid id, UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        var article = await FindOrThrowAsync(id, cancellationToken);
        await EnsureReferencesExistAsync(request.CategoryId, request.TagIds, cancellationToken);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? article.Slug
            : await SlugResolver.ResolveAsync(request.Slug, request.Title, "article",
                _db.Articles.Where(a => a.Id != id).Select(a => a.Slug), cancellationToken);

        article.Update(ToDetails(request.Title, slug, request.Summary, request.Content, request.FeaturedMediaId,
            request.CategoryId, request.AuthorName, request.Status), _dateTimeProvider.UtcNow);
        article.ReplaceTags(request.TagIds ?? []);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(article);
    }

    /// <summary>Hard delete; the article's tag links go with it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await FindOrThrowAsync(id, cancellationToken);
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Article> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Articles.Include(a => a.Tags).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Article), id);

    private async Task EnsureReferencesExistAsync(Guid? categoryId, IReadOnlyList<Guid>? tagIds, CancellationToken cancellationToken)
    {
        if (categoryId is { } category && !await _db.ArticleCategories.AnyAsync(c => c.Id == category, cancellationToken))
        {
            throw new BusinessRuleValidationException("The category does not exist.");
        }

        var requested = (tagIds ?? []).Distinct().ToList();
        if (requested.Count > 0 && await _db.ArticleTags.CountAsync(t => requested.Contains(t.Id), cancellationToken) != requested.Count)
        {
            throw new BusinessRuleValidationException("One or more tags do not exist.");
        }
    }

    private ArticleDetails ToDetails(
        string title, string slug, string summary, string content, string? featuredMediaId, Guid? categoryId,
        string authorName, string status)
    {
        // The validators already checked the status; a failed parse here would be a programming error.
        ContentWireFormat.TryParseStatus(status, out var parsedStatus);

        return new ArticleDetails(title, slug, summary, _sanitizer.Sanitize(content), featuredMediaId, categoryId, authorName, parsedStatus);
    }

    private static ArticleResponse ToResponse(Article article) => new(
        article.Id, article.Title, article.Slug, article.Summary, article.Content, article.FeaturedMediaId, article.CategoryId,
        article.Tags.Select(link => link.TagId).ToList(), article.AuthorName, ContentWireFormat.ToWire(article.Status),
        article.PublishedAtUtc, article.CreatedAtUtc, article.UpdatedAtUtc);
}

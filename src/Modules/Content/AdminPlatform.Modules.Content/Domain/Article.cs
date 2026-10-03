using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>A news/recipe article. <see cref="Content"/> is HTML from the admin's rich-text editor and is
/// sanitized by the Application layer before it reaches this entity (the website renders it as HTML).</summary>
public sealed class Article : AuditableEntity
{
    public const int MaxTitleLength = 300;
    public const int MaxSummaryLength = 1000;
    public const int MaxAuthorLength = 200;
    public const int MaxMediaIdLength = 64;

    private readonly List<ArticleTagLink> _tags = [];

    public string Title { get; private set; } = string.Empty;

    /// <summary>Unique; the public URL is /bai-viet/{slug}.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    /// <summary>Opaque media reference with no FK (a Media module id, or a legacy key the website resolves
    /// itself) — modules never share tables (ARCHITECTURE.md). Same convention as ProductMedia.</summary>
    public string? FeaturedMediaId { get; private set; }

    public Guid? CategoryId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public PublishStatus Status { get; private set; } = PublishStatus.Draft;

    /// <summary>Set the first time the article is published and kept afterwards (un-publishing does not erase it).</summary>
    public DateTime? PublishedAtUtc { get; private set; }

    public IReadOnlyList<ArticleTagLink> Tags => _tags;

    private Article()
    {
        // EF Core
    }

    public static Article Create(ArticleDetails details, DateTime nowUtc)
    {
        var article = new Article { Id = Guid.NewGuid() };
        article.Update(details, nowUtc);
        return article;
    }

    public void Update(ArticleDetails details, DateTime nowUtc)
    {
        if (!SharedKernel.Slug.IsValid(details.Slug))
        {
            throw new BusinessRuleValidationException($"'{details.Slug}' is not a valid slug.");
        }

        Title = Guard.NotNullOrWhiteSpace(details.Title, nameof(details.Title)).Trim();
        Slug = details.Slug;
        Summary = Guard.NotNullOrWhiteSpace(details.Summary, nameof(details.Summary)).Trim();
        Content = Guard.NotNullOrWhiteSpace(details.Content, nameof(details.Content));
        FeaturedMediaId = string.IsNullOrWhiteSpace(details.FeaturedMediaId) ? null : details.FeaturedMediaId.Trim();
        CategoryId = details.CategoryId;
        AuthorName = Guard.NotNullOrWhiteSpace(details.AuthorName, nameof(details.AuthorName)).Trim();
        Status = details.Status;

        if (details.Status == PublishStatus.Published && PublishedAtUtc is null)
        {
            PublishedAtUtc = nowUtc;
        }
    }

    /// <summary>Replaces the tag list. Links still present keep their row so EF never deletes and
    /// re-inserts the same composite key in one save.</summary>
    public void ReplaceTags(IReadOnlyCollection<Guid> tagIds)
    {
        var ids = tagIds.Distinct().ToList();

        _tags.RemoveAll(link => !ids.Contains(link.TagId));
        foreach (var id in ids.Where(id => !_tags.Exists(link => link.TagId == id)))
        {
            _tags.Add(ArticleTagLink.Create(Id, id));
        }
    }

    /// <summary>Seeding only: lets imported content keep the date it was originally published.</summary>
    public void SetPublishedAt(DateTime? publishedAtUtc) => PublishedAtUtc = publishedAtUtc;
}

/// <summary>The editable scalar fields of an article, grouped so Create/Update stay readable.</summary>
public sealed record ArticleDetails(
    string Title,
    string Slug,
    string Summary,
    string Content,
    string? FeaturedMediaId,
    Guid? CategoryId,
    string AuthorName,
    PublishStatus Status);

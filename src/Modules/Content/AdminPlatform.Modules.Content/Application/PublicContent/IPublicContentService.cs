using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.PublicContent;

/// <summary>Read-only, anonymous view of the content for the public website: only Published articles and
/// only the fields the website needs.</summary>
public interface IPublicContentService
{
    /// <summary>Published articles, newest first.</summary>
    Task<PagedResult<PublicArticleSummaryResponse>> ListArticlesAsync(PagedRequest request, CancellationToken cancellationToken);

    /// <summary>404 (NotFoundException) unless the article exists AND is published.</summary>
    Task<PublicArticleResponse> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<PublicTaxonomyResponse> GetTaxonomyAsync(CancellationToken cancellationToken);
}

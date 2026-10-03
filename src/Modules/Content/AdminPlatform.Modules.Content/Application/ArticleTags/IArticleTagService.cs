using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.ArticleTags;

public interface IArticleTagService
{
    Task<PagedResult<ArticleTagResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<ArticleTagResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ArticleTagResponse> CreateAsync(CreateArticleTagRequest request, CancellationToken cancellationToken);

    Task<ArticleTagResponse> UpdateAsync(Guid id, UpdateArticleTagRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

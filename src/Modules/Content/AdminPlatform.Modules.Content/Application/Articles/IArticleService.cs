using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.Articles;

public interface IArticleService
{
    /// <param name="status">Filters by status ("draft" | "published" | "archived").</param>
    Task<PagedResult<ArticleListItemResponse>> ListAsync(
        PagedRequest request, string? status, Guid? categoryId, CancellationToken cancellationToken);

    Task<ArticleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken);

    Task<ArticleResponse> UpdateAsync(Guid id, UpdateArticleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

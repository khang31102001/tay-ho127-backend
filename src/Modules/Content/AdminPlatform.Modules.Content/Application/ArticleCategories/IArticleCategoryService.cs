using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.ArticleCategories;

public interface IArticleCategoryService
{
    Task<PagedResult<ArticleCategoryResponse>> ListAsync(PagedRequest request, Guid? parentId, bool? isActive, CancellationToken cancellationToken);

    Task<ArticleCategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ArticleCategoryResponse> CreateAsync(CreateArticleCategoryRequest request, CancellationToken cancellationToken);

    Task<ArticleCategoryResponse> UpdateAsync(Guid id, UpdateArticleCategoryRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

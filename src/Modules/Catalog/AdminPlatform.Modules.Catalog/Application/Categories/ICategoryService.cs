using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.Categories;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> ListAsync(PagedRequest request, Guid? parentId, bool? isActive, CancellationToken cancellationToken);

    Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

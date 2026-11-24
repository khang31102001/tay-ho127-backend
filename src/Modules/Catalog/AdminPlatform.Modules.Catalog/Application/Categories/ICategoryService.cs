using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.Categories;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> ListAsync(PagedRequest request, Guid? parentId, bool? isActive, CancellationToken cancellationToken);

    Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>Số danh mục con và sản phẩm sẽ bị ảnh hưởng nếu xóa danh mục (để Admin xem trước khi xác nhận).</summary>
    Task<CategoryDeleteImpactResponse> GetDeleteImpactAsync(Guid id, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

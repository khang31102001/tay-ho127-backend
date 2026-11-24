namespace AdminPlatform.Modules.Catalog.Application.Categories;

public sealed record CreateCategoryRequest(string Name, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record UpdateCategoryRequest(string Name, Guid? ParentId, int SortOrder, bool IsActive);

/// <summary>Xóa danh mục: ChildCount danh mục con dời lên cấp trên, ProductCount sản phẩm chuyển sang "chưa phân loại".</summary>
public sealed record CategoryDeleteImpactResponse(int ChildCount, int ProductCount);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

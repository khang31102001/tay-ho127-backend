namespace AdminPlatform.Modules.Catalog.Application.Categories;

public sealed record CreateCategoryRequest(string Name, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record UpdateCategoryRequest(string Name, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

namespace AdminPlatform.Modules.Content.Application.ArticleCategories;

/// <summary>Slug is optional: when blank it is generated from Name (and made unique).</summary>
public sealed record CreateArticleCategoryRequest(string Name, string? Slug, Guid? ParentId, int SortOrder, bool IsActive);

/// <summary>A blank slug keeps the current one: renaming must not silently change a public URL.</summary>
public sealed record UpdateArticleCategoryRequest(string Name, string? Slug, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record ArticleCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

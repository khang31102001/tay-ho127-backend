using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Article taxonomy: a self-referencing tree (up to <see cref="MaxDepth"/> levels). Kept apart from
/// the catalog categories on purpose — news/recipes and food/drinks are unrelated taxonomies. Depth is
/// enforced by ArticleCategoryService, since it depends on the rest of the tree.</summary>
public sealed class ArticleCategory : AuditableEntity
{
    public const int MaxDepth = 3;
    public const int MaxNameLength = 200;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Unique; the website filters articles by it.</summary>
    public string Slug { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ArticleCategory()
    {
        // EF Core
    }

    public static ArticleCategory Create(string name, string slug, Guid? parentId, int sortOrder, bool isActive)
    {
        var category = new ArticleCategory { Id = Guid.NewGuid() };
        category.Update(name, slug, parentId, sortOrder, isActive);
        return category;
    }

    public void Update(string name, string slug, Guid? parentId, int sortOrder, bool isActive)
    {
        if (!SharedKernel.Slug.IsValid(slug))
        {
            throw new BusinessRuleValidationException($"'{slug}' is not a valid slug.");
        }

        if (parentId == Id)
        {
            throw new BusinessRuleValidationException("A category cannot be its own parent.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        Slug = slug;
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
    }
}

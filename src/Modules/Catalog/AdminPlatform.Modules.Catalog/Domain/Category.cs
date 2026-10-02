using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>Catalog taxonomy: one self-referencing table holding the 3-level tree
/// group → category → sub-category (products normally hang off a leaf). Depth is enforced by
/// CategoryService, since it depends on the rest of the tree.</summary>
public sealed class Category : AuditableEntity
{
    /// <summary>group → category → sub-category.</summary>
    public const int MaxDepth = 3;

    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Category()
    {
        // EF Core
    }

    public static Category Create(string name, Guid? parentId, int sortOrder, bool isActive)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = isActive,
        };
    }

    public void Update(string name, Guid? parentId, int sortOrder, bool isActive)
    {
        if (parentId == Id)
        {
            throw new BusinessRuleValidationException("A category cannot be its own parent.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
    }
}

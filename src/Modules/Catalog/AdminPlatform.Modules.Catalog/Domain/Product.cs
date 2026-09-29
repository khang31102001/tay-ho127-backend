using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A sellable dish/item. Images (ordered) and modifier groups (ordered) are N-N links owned by
/// the product and always replaced as a whole list from the admin editor.</summary>
public sealed class Product : AuditableEntity
{
    public const int MaxMediaCount = 20;

    private readonly List<ProductMedia> _media = [];
    private readonly List<ProductModifierGroup> _modifierGroups = [];

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public decimal Price { get; private set; }

    /// <summary>Pre-discount price shown struck through next to <see cref="Price"/>.</summary>
    public decimal? OldPrice { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Short label also used for site search matching (e.g. "DISH").</summary>
    public string? Badge { get; private set; }

    /// <summary>Average rating on a 0–5 scale, display only.</summary>
    public decimal? Rating { get; private set; }

    public int? RatingCount { get; private set; }

    public IReadOnlyList<ProductMedia> Media => _media;
    public IReadOnlyList<ProductModifierGroup> ModifierGroups => _modifierGroups;

    private Product()
    {
        // EF Core
    }

    public static Product Create(string name, string slug, Guid categoryId, ProductDetails details)
    {
        var product = new Product { Id = Guid.NewGuid() };
        product.Update(name, slug, categoryId, details);
        return product;
    }

    public void Update(string name, string slug, Guid categoryId, ProductDetails details)
    {
        if (!Domain.Slug.IsValid(slug))
        {
            throw new BusinessRuleValidationException($"'{slug}' is not a valid slug.");
        }

        if (details.Price < 0 || details.OldPrice < 0)
        {
            throw new BusinessRuleValidationException("Prices cannot be negative.");
        }

        if (details.Rating is < 0 or > 5)
        {
            throw new BusinessRuleValidationException("Rating must be between 0 and 5.");
        }

        if (details.RatingCount < 0)
        {
            throw new BusinessRuleValidationException("Rating count cannot be negative.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        Slug = slug;
        CategoryId = Guard.NotEmpty(categoryId, nameof(categoryId));
        Price = details.Price;
        OldPrice = details.OldPrice;
        Description = string.IsNullOrWhiteSpace(details.Description) ? null : details.Description.Trim();
        IsActive = details.IsActive;
        Badge = string.IsNullOrWhiteSpace(details.Badge) ? null : details.Badge.Trim();
        Rating = details.Rating;
        RatingCount = details.RatingCount;
    }

    /// <summary>Replaces the ordered image list. Links still present keep their row (only the sort order
    /// changes), so EF never deletes and re-inserts the same composite key in one save.</summary>
    public void ReplaceMedia(IReadOnlyList<string> mediaIds)
    {
        var orderedIds = mediaIds.Select(id => id.Trim()).Where(id => id.Length > 0).Distinct().ToList();
        if (orderedIds.Count > MaxMediaCount)
        {
            throw new BusinessRuleValidationException($"A product can have at most {MaxMediaCount} images.");
        }

        _media.RemoveAll(link => !orderedIds.Contains(link.MediaId));
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var existing = _media.Find(link => link.MediaId == orderedIds[index]);
            if (existing is null)
            {
                _media.Add(ProductMedia.Create(Id, orderedIds[index], index));
            }
            else
            {
                existing.MoveTo(index);
            }
        }
    }

    /// <summary>Same keep-or-add strategy as <see cref="ReplaceMedia"/>.</summary>
    public void ReplaceModifierGroups(IReadOnlyList<Guid> modifierGroupIds)
    {
        var orderedIds = modifierGroupIds.Distinct().ToList();

        _modifierGroups.RemoveAll(link => !orderedIds.Contains(link.ModifierGroupId));
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var existing = _modifierGroups.Find(link => link.ModifierGroupId == orderedIds[index]);
            if (existing is null)
            {
                _modifierGroups.Add(ProductModifierGroup.Create(Id, orderedIds[index], index));
            }
            else
            {
                existing.MoveTo(index);
            }
        }
    }
}

/// <summary>The editable scalar fields of a product, grouped so Create/Update stay readable.</summary>
public sealed record ProductDetails(
    decimal Price,
    decimal? OldPrice,
    string? Description,
    bool IsActive,
    string? Badge,
    decimal? Rating,
    int? RatingCount);

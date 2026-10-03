using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>A flat label an article can carry (many per article). No parent or status: a tag is just a
/// name and a slug.</summary>
public sealed class ArticleTag : AuditableEntity
{
    public const int MaxNameLength = 100;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Unique.</summary>
    public string Slug { get; private set; } = string.Empty;

    private ArticleTag()
    {
        // EF Core
    }

    public static ArticleTag Create(string name, string slug)
    {
        var tag = new ArticleTag { Id = Guid.NewGuid() };
        tag.Update(name, slug);
        return tag;
    }

    public void Update(string name, string slug)
    {
        if (!SharedKernel.Slug.IsValid(slug))
        {
            throw new BusinessRuleValidationException($"'{slug}' is not a valid slug.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        Slug = slug;
    }
}

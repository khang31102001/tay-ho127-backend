namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Link between an article and a tag it carries (no extra fields, so no entity of its own beyond the key).</summary>
public sealed class ArticleTagLink
{
    public Guid ArticleId { get; private set; }
    public Guid TagId { get; private set; }

    private ArticleTagLink()
    {
        // EF Core
    }

    internal static ArticleTagLink Create(Guid articleId, Guid tagId) => new() { ArticleId = articleId, TagId = tagId };
}

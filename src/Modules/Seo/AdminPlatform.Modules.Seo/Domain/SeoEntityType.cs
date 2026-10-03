namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>The kinds of website content an SEO override can attach to. Homepage is a singleton (no entity id).
/// On the wire the lowercase name is used ("product", "category", "article", "page", "homepage").</summary>
public enum SeoEntityType
{
    Product,
    Category,
    Article,
    Page,
    Homepage,
}

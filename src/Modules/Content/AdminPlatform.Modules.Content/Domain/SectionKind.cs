namespace AdminPlatform.Modules.Content.Domain;

/// <summary>The kind of block a page section is. Wire names are the lowercase enum names
/// ("hero", "introduction", "promotion", "highlight", "testimonial", "cta").</summary>
public enum SectionKind
{
    Hero,
    Introduction,
    Promotion,
    Highlight,
    Testimonial,
    Cta,
}

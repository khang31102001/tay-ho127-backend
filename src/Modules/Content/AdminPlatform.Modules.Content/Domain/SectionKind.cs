namespace AdminPlatform.Modules.Content.Domain;

/// <summary>The kind of block a page section is. Wire names are the lowercase enum names
/// ("hero", "introduction", "promotion", "highlight", "testimonial", "cta", "story", "award", "stats", "timeline",
/// "faq", "core").
/// Story is the homepage brand story block (heading, paragraphs, main image + gallery, CTA).
/// Award is the homepage award card (heading, text, main image, badge as the first gallery image, optional CTA).
/// Stats, Timeline and Faq carry their rows in <see cref="PageSectionSettings.Items"/>.
/// Core is a page's main content rendered from another module (menu grid, article list…): the section only holds
/// its position and optional heading.</summary>
public enum SectionKind
{
    Hero,
    Introduction,
    Promotion,
    Highlight,
    Testimonial,
    Cta,
    Story,
    Award,
    Stats,
    Timeline,
    Faq,
    Core,
}

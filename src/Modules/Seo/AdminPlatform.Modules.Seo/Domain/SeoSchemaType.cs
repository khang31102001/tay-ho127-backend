namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>The Schema.org types an SEO schema override can target. The member name is also the wire value
/// ("Product", "FAQPage", ...). "Offer" is normally generated nested inside Product; it is addressable on its own
/// so it can be overridden separately later.</summary>
public enum SeoSchemaType
{
    Organization,
    LocalBusiness,
    Restaurant,
    WebSite,
    WebPage,
    Product,
    BreadcrumbList,
    FAQPage,
    Article,
    Offer,
}

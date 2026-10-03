namespace AdminPlatform.Modules.Seo.Application.Schemas;

/// <summary>EntityType: "product" | "category" | "article" | "page" | "homepage"; EntityId is required except for
/// "homepage". SchemaType: "Organization" | "LocalBusiness" | "Restaurant" | "WebSite" | "WebPage" | "Product" |
/// "BreadcrumbList" | "FAQPage" | "Article" | "Offer" (case-insensitive). Config: extra string fields the entity
/// does not have (e.g. sku, brand); blank values are dropped. CustomJsonLd: a JSON object/array, required when
/// IsCustomOverride is on (Advanced Mode replaces the generated schema).</summary>
public sealed record UpsertSeoSchemaRequest(
    string EntityType,
    string? EntityId,
    string SchemaType,
    Dictionary<string, string>? Config,
    string? CustomJsonLd,
    bool IsCustomOverride,
    bool IsActive);

public sealed record SeoSchemaResponse(
    Guid Id,
    string EntityType,
    string? EntityId,
    string SchemaType,
    IReadOnlyDictionary<string, string>? Config,
    string? CustomJsonLd,
    bool IsCustomOverride,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

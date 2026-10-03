using AdminPlatform.Modules.Seo.Domain;

namespace AdminPlatform.Modules.Seo.Application.Schemas;

/// <summary>Maps <see cref="SeoSchemaType"/> to/from its wire name (the member name, e.g. "FAQPage").</summary>
public static class SeoSchemaTypeWire
{
    public const string AllowedValues =
        "Organization, LocalBusiness, Restaurant, WebSite, WebPage, Product, BreadcrumbList, FAQPage, Article or Offer";

    public static string ToWire(SeoSchemaType schemaType) => schemaType.ToString();

    /// <summary>Rejects numeric strings ("1") that Enum.TryParse would otherwise accept.</summary>
    public static bool TryParse(string? value, out SeoSchemaType schemaType)
    {
        schemaType = default;
        var trimmed = value?.Trim();
        return !string.IsNullOrEmpty(trimmed) && trimmed.All(char.IsLetter)
            && Enum.TryParse(trimmed, ignoreCase: true, out schemaType) && Enum.IsDefined(schemaType);
    }
}

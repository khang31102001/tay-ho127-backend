using AdminPlatform.Modules.Seo.Domain;

namespace AdminPlatform.Modules.Seo.Application.Metadata;

/// <summary>Maps <see cref="SeoEntityType"/> to/from the lowercase names used on the wire.</summary>
public static class SeoEntityTypeWire
{
    public const string AllowedValues = "product, category, article, page or homepage";

    public static string ToWire(SeoEntityType entityType) => entityType.ToString().ToLowerInvariant();

    /// <summary>Rejects numeric strings ("1") that Enum.TryParse would otherwise accept.</summary>
    public static bool TryParse(string? value, out SeoEntityType entityType)
    {
        entityType = default;
        var trimmed = value?.Trim();
        return !string.IsNullOrEmpty(trimmed) && trimmed.All(char.IsLetter)
            && Enum.TryParse(trimmed, ignoreCase: true, out entityType) && Enum.IsDefined(entityType);
    }
}

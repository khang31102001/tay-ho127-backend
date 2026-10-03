using System.Text.Json;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Seo.Domain;

/// <summary>Schema.org / JSON-LD configuration for one entity and one schema type. It does NOT store the generated
/// schema — the website derives that from the entity's own data; only what cannot be derived is kept: extra
/// fields (<see cref="Config"/>, e.g. a product's SKU and brand) or, in Advanced Mode, a hand-written JSON-LD
/// (<see cref="CustomJsonLd"/>) that replaces the generated one. One row per (EntityType, EntityId, SchemaType).</summary>
public sealed class SeoSchema : AuditableEntity
{
    public const int MaxCustomJsonLdLength = 50_000;
    public const int MaxConfigEntries = 20;
    public const int MaxConfigKeyLength = 50;
    public const int MaxConfigValueLength = 500;

    public SeoEntityType EntityType { get; private set; }

    /// <summary>Same convention as <see cref="SeoMetadata.EntityId"/>: the entity's Backend id, no FK; null only for the homepage.</summary>
    public string? EntityId { get; private set; }

    public SeoSchemaType SchemaType { get; private set; }

    /// <summary>Extra fields the entity does not have, as a JSON object of strings (e.g. {"sku":"BC-001"}); null = none.</summary>
    public string? ConfigJson { get; private set; }

    /// <summary>A JSON object or array (JSON-LD). Only used by the website when <see cref="IsCustomOverride"/> is on.</summary>
    public string? CustomJsonLd { get; private set; }

    public bool IsCustomOverride { get; private set; }
    public bool IsActive { get; private set; } = true;

    private SeoSchema()
    {
        // EF Core
    }

    public static SeoSchema Create(SeoEntityType entityType, string? entityId, SeoSchemaType schemaType, SeoSchemaDetails details)
    {
        var schema = new SeoSchema
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = SeoMetadata.NormalizeEntityId(entityType, entityId),
            SchemaType = schemaType,
        };
        schema.Update(details);
        return schema;
    }

    public void Update(SeoSchemaDetails details)
    {
        var customJsonLd = string.IsNullOrWhiteSpace(details.CustomJsonLd) ? null : details.CustomJsonLd.Trim();

        if (customJsonLd is not null && !IsValidJsonLd(customJsonLd))
        {
            throw new BusinessRuleValidationException("The custom JSON-LD must be valid JSON (an object or an array).");
        }

        if (details.IsCustomOverride && customJsonLd is null)
        {
            throw new BusinessRuleValidationException("Advanced Mode needs a custom JSON-LD to replace the generated one.");
        }

        ConfigJson = SerializeConfig(details.Config);
        CustomJsonLd = customJsonLd;
        IsCustomOverride = details.IsCustomOverride;
        IsActive = details.IsActive;
    }

    /// <summary>The config as a dictionary (empty when none) — the inverse of the JSON kept in <see cref="ConfigJson"/>.</summary>
    public IReadOnlyDictionary<string, string> Config =>
        ConfigJson is null ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(ConfigJson) ?? [];

    /// <summary>Valid JSON whose root is an object or an array — a bare string or number is not JSON-LD.</summary>
    public static bool IsValidJsonLd(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Drops blank values and trims keys/values; at most <see cref="MaxConfigEntries"/> entries.</summary>
    private static string? SerializeConfig(IReadOnlyDictionary<string, string>? config)
    {
        var cleaned = (config ?? new Dictionary<string, string>())
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value.Trim());

        if (cleaned.Count > MaxConfigEntries)
        {
            throw new BusinessRuleValidationException($"At most {MaxConfigEntries} config fields are allowed.");
        }

        if (cleaned.Any(pair => pair.Key.Length > MaxConfigKeyLength || pair.Value.Length > MaxConfigValueLength))
        {
            throw new BusinessRuleValidationException("A config field name or value is too long.");
        }

        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
    }
}

/// <summary>The editable fields of an SEO schema, grouped so Create/Update stay readable.</summary>
public sealed record SeoSchemaDetails(
    IReadOnlyDictionary<string, string>? Config,
    string? CustomJsonLd,
    bool IsCustomOverride,
    bool IsActive);

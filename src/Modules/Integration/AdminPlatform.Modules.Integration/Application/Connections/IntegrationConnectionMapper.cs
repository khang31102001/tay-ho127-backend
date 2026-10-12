using System.Text.Json;
using AdminPlatform.Modules.Integration.Application.Providers;
using AdminPlatform.Modules.Integration.Domain;

namespace AdminPlatform.Modules.Integration.Application.Connections;

/// <summary>Shared translation between a stored connection and its API / snapshot forms.</summary>
internal static class IntegrationConnectionMapper
{
    public static string ToWire(IntegrationEnvironment environment) => environment.ToString().ToLowerInvariant();

    public static bool TryParseEnvironment(string? value, out IntegrationEnvironment environment) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out environment) && Enum.IsDefined(environment);

    public static IReadOnlyDictionary<string, string> ReadConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(configJson) ?? [];
        }
        catch (JsonException)
        {
            // A hand-edited row with unreadable JSON is treated as "no settings" rather than failing every request.
            return new Dictionary<string, string>();
        }
    }

    public static string WriteConfig(IReadOnlyDictionary<string, string> config) =>
        JsonSerializer.Serialize(config.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value));

    public static IntegrationConnectionResponse ToResponse(
        IntegrationConnection connection, IntegrationProviderDefinition? provider, IReadOnlyDictionary<string, string> setSecrets)
    {
        var missing = provider is null
            ? []
            : provider.SecretConfigPaths.Keys.Where(name => !setSecrets.ContainsKey(name)).Order(StringComparer.Ordinal).ToList();

        return new IntegrationConnectionResponse(
            connection.Id,
            connection.ProviderCode,
            connection.DisplayName,
            connection.IsEnabled,
            ToWire(connection.Environment),
            ReadConfig(connection.ConfigJson),
            provider?.ConfigKeys ?? [],
            missing,
            missing.Count == 0,
            connection.UpdatedAtUtc);
    }
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Integration.Domain;

/// <summary>How the system connects to one third-party provider (e.g. "esms"): on/off, sandbox or live, and the
/// non-secret settings the provider needs (Zalo OA id, brandname, template ids...). It deliberately holds NO
/// secret: API keys come from environment configuration and never reach this table or any API response.</summary>
public sealed class IntegrationConnection : AuditableEntity
{
    public const int MaxProviderCodeLength = 64;
    public const int MaxDisplayNameLength = 200;

    /// <summary>Stable lower-case key the rest of the system looks the provider up by.</summary>
    public string ProviderCode { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public IntegrationEnvironment Environment { get; private set; } = IntegrationEnvironment.Sandbox;

    /// <summary>JSON object of non-secret provider settings. Which keys are allowed is decided per provider by the
    /// Application layer, not here.</summary>
    public string ConfigJson { get; private set; } = "{}";

    private IntegrationConnection()
    {
        // EF Core
    }

    /// <summary>A new connection starts disabled: it must be switched on on purpose once its secrets are in place.</summary>
    public static IntegrationConnection Create(string providerCode, string displayName, IntegrationEnvironment environment, string configJson)
    {
        return new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            ProviderCode = Guard.NotNullOrWhiteSpace(providerCode, nameof(providerCode)).Trim().ToLowerInvariant(),
            DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim(),
            IsEnabled = false,
            Environment = environment,
            ConfigJson = NormalizeConfig(configJson),
        };
    }

    public void Update(string displayName, bool isEnabled, IntegrationEnvironment environment, string configJson)
    {
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();
        IsEnabled = isEnabled;
        Environment = environment;
        ConfigJson = NormalizeConfig(configJson);
    }

    private static string NormalizeConfig(string? configJson) => string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
}

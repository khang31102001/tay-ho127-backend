namespace AdminPlatform.Modules.Integration.Application.Connections;

/// <summary>Environment: "sandbox" | "live". Config holds only the provider's non-secret settings; unknown keys are
/// rejected. A connection cannot be enabled while its secrets are missing from the server configuration.</summary>
public sealed record UpdateIntegrationConnectionRequest(
    string DisplayName,
    bool IsEnabled,
    string Environment,
    Dictionary<string, string>? Config);

/// <summary>Never carries a secret value. MissingSecrets names the secrets (e.g. "apiKey") not yet set in the
/// server environment, so an admin knows what to ask the operator for.</summary>
public sealed record IntegrationConnectionResponse(
    Guid Id,
    string ProviderCode,
    string DisplayName,
    bool IsEnabled,
    string Environment,
    IReadOnlyDictionary<string, string> Config,
    IReadOnlyList<string> AllowedConfigKeys,
    IReadOnlyList<string> MissingSecrets,
    bool SecretsConfigured,
    DateTime? UpdatedAtUtc);

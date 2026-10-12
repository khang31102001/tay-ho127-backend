namespace AdminPlatform.Modules.Integration.Application.Providers;

/// <summary>A third-party provider the system knows how to connect to. <see cref="ConfigKeys"/> are the only
/// non-secret settings an admin may store for it; <see cref="SecretConfigPaths"/> maps each secret's short name to
/// the server configuration key (environment variable) it is read from — secrets never live in the database.</summary>
public sealed record IntegrationProviderDefinition(
    string Code,
    string DisplayName,
    IReadOnlyList<string> ConfigKeys,
    IReadOnlyDictionary<string, string> SecretConfigPaths);

/// <summary>The fixed list of providers. Adding one means adding it here; the seeder then creates its (disabled)
/// connection row and the admin API starts accepting its settings.</summary>
public static class IntegrationProviders
{
    public const string Esms = "esms";

    public static IReadOnlyList<IntegrationProviderDefinition> All { get; } =
    [
        new(
            Esms,
            "eSMS (Zalo ZNS + SMS)",
            ConfigKeys:
            [
                "oaId",
                "brandname",
                "otpRegisterTemplateId",
                "orderPlacedTemplateId",
                "otpRegisterSmsContent",
                "smsType",
            ],
            SecretConfigPaths: new Dictionary<string, string>
            {
                ["apiKey"] = "Integrations:Esms:ApiKey",
                ["secretKey"] = "Integrations:Esms:SecretKey",
            }),
    ];

    public static IntegrationProviderDefinition? Find(string providerCode) =>
        All.FirstOrDefault(p => string.Equals(p.Code, providerCode, StringComparison.OrdinalIgnoreCase));
}

namespace AdminPlatform.Modules.Integration.Api;

public static class IntegrationPermissions
{
    public const string IntegrationsView = "integrations.view";

    /// <summary>Switches a provider on/off, between sandbox and live, and edits its non-secret settings.
    /// A powerful permission: a live connection sends real messages that cost money.</summary>
    public const string IntegrationsUpdate = "integrations.update";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (IntegrationsView, "View third-party integrations and their settings"),
        (IntegrationsUpdate, "Enable, disable and configure third-party integrations"),
    ];
}

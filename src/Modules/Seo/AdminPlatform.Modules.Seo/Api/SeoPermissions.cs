namespace AdminPlatform.Modules.Seo.Api;

public static class SeoPermissions
{
    public const string SeoSettingsView = "seo-settings.view";
    public const string SeoSettingsUpdate = "seo-settings.update";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (SeoSettingsView, "View SEO settings"),
        (SeoSettingsUpdate, "Update SEO settings"),
    ];
}

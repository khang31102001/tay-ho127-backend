namespace AdminPlatform.Modules.Seo.Api;

public static class SeoPermissions
{
    public const string SeoSettingsView = "seo-settings.view";
    public const string SeoSettingsUpdate = "seo-settings.update";

    public const string SeoMetadataView = "seo-metadata.view";

    /// <summary>Saving an override (create or replace) — an override is upserted per entity, so there is no separate create.</summary>
    public const string SeoMetadataUpdate = "seo-metadata.update";

    public const string SeoMetadataDelete = "seo-metadata.delete";

    public const string RedirectsView = "redirects.view";
    public const string RedirectsCreate = "redirects.create";
    public const string RedirectsUpdate = "redirects.update";
    public const string RedirectsDelete = "redirects.delete";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (SeoSettingsView, "View SEO settings"),
        (SeoSettingsUpdate, "Update SEO settings"),
        (SeoMetadataView, "View SEO metadata overrides"),
        (SeoMetadataUpdate, "Create or update SEO metadata overrides"),
        (SeoMetadataDelete, "Reset (delete) SEO metadata overrides"),
        (RedirectsView, "View redirects"),
        (RedirectsCreate, "Create redirects"),
        (RedirectsUpdate, "Update redirects"),
        (RedirectsDelete, "Delete redirects"),
    ];
}

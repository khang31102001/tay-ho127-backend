namespace AdminPlatform.Modules.Navigation.Application;

/// <summary>Bound from the "Navigation" configuration section.</summary>
public sealed class NavigationOptions
{
    public const string SectionName = "Navigation";

    /// <summary>Whether permissions may be assigned to WEBSITE items. Off: the website is public and the schema is
    /// ready for it, but nothing can gate a website item until visitors can hold permissions (customer accounts).
    /// Even when on, the anonymous endpoint never returns an item that has permission rows.</summary>
    public bool AllowSitePermissions { get; set; }
}

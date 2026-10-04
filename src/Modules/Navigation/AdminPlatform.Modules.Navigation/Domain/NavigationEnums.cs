namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>Who a navigation menu is for. ADMIN = the admin sidebar (needs a login, filtered by permission).
/// SITE = the public website menus (anonymous by default).</summary>
public enum NavigationScope
{
    Admin,
    Site,
}

/// <summary>Where a menu is shown. Admin menus only use Sidebar; Site menus use Header/Footer/Mobile.</summary>
public enum NavigationLocation
{
    Sidebar,
    Header,
    Footer,
    Mobile,
}

/// <summary>What a SITE item points to: an internal route, a CMS page, or an external URL.</summary>
public enum NavigationTargetType
{
    Route,
    Page,
    External,
}

namespace AdminPlatform.Modules.Navigation.Application.Public;

/// <summary>Anonymous read of the website menus. Never returns the admin sidebar, an inactive menu's items, an inactive
/// item (with its whole branch) or an item that has permission rows — those are not public.</summary>
public interface IPublicNavigationService
{
    /// <summary>Location is "header" | "footer" | "mobile". An unknown location is a 404; a missing or switched-off menu
    /// yields an empty item list.</summary>
    Task<PublicNavigationResponse> GetByLocationAsync(string location, CancellationToken cancellationToken);
}

using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>A navigation container: the admin sidebar, or one of the website menus (header/footer/mobile).
/// Scope and Location never change after creation; at most one menu exists per (scope, location).</summary>
public sealed class NavigationMenu : CatalogEntity
{
    public NavigationScope Scope { get; private set; }
    public NavigationLocation Location { get; private set; }

    private NavigationMenu()
    {
        // EF Core
    }

    public static NavigationMenu Create(string code, string name, NavigationScope scope, NavigationLocation location)
    {
        if (!IsValidPlacement(scope, location))
        {
            throw new BusinessRuleValidationException($"A {scope} menu cannot be placed at '{location}'.");
        }

        return new NavigationMenu
        {
            Id = Guid.NewGuid(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            Scope = scope,
            Location = location,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
    }

    public static bool IsValidPlacement(NavigationScope scope, NavigationLocation location) =>
        scope == NavigationScope.Admin
            ? location == NavigationLocation.Sidebar
            : location is NavigationLocation.Header or NavigationLocation.Footer or NavigationLocation.Mobile;
}

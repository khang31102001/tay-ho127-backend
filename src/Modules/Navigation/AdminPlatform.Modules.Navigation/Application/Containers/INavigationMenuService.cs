namespace AdminPlatform.Modules.Navigation.Application.Containers;

/// <summary>The navigation containers (admin sidebar, website header/footer/mobile). They are fixed fixtures created
/// by the seed — only the name and the on/off switch can be edited.</summary>
public interface INavigationMenuService
{
    /// <summary>Containers of every scope the caller may view.</summary>
    Task<IReadOnlyList<NavigationMenuResponse>> ListAsync(CancellationToken cancellationToken);

    Task<NavigationMenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<NavigationMenuResponse> UpdateAsync(Guid id, UpdateNavigationMenuRequest request, CancellationToken cancellationToken);
}

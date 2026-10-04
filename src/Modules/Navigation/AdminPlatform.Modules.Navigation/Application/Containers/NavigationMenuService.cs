using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application.Containers;

public sealed class NavigationMenuService : INavigationMenuService
{
    private readonly INavigationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public NavigationMenuService(INavigationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NavigationMenuResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var viewableScopes = Enum.GetValues<NavigationScope>()
            .Where(scope => NavigationAccess.Can(_currentUser, scope, NavigationAction.View))
            .ToList();
        if (viewableScopes.Count == 0)
        {
            throw new ForbiddenException("Bạn không có quyền xem menu.");
        }

        var menus = await _db.NavigationMenus.AsNoTracking()
            .Where(m => viewableScopes.Contains(m.Scope))
            .OrderBy(m => m.Scope).ThenBy(m => m.Location)
            .ToListAsync(cancellationToken);

        return menus.Select(ToResponse).ToList();
    }

    public async Task<NavigationMenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.View);
        return ToResponse(menu);
    }

    public async Task<NavigationMenuResponse> UpdateAsync(Guid id, UpdateNavigationMenuRequest request, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);
        NavigationAccess.Require(_currentUser, menu.Scope, NavigationAction.Update);

        menu.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menu);
    }

    private async Task<NavigationMenu> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.NavigationMenus.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(NavigationMenu), id);

    private static NavigationMenuResponse ToResponse(NavigationMenu menu) =>
        new(menu.Id, menu.Code, menu.Name, menu.IsActive, menu.Scope.ToString().ToLowerInvariant(), menu.Location.ToString().ToLowerInvariant());
}

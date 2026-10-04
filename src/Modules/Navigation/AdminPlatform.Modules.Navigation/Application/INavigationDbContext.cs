using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application;

public interface INavigationDbContext
{
    DbSet<NavigationMenu> NavigationMenus { get; }
    DbSet<NavigationItem> NavigationItems { get; }
    DbSet<NavigationItemSiteDetail> NavigationItemSiteDetails { get; }
    DbSet<NavigationItemPermission> NavigationItemPermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

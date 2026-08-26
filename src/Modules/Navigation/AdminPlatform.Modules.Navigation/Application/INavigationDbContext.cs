using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application;

public interface INavigationDbContext
{
    DbSet<Menu> Menus { get; }
    DbSet<MenuPermission> MenuPermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

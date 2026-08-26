using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

public sealed class NavigationDbContext : DbContext, INavigationDbContext
{
    public const string Schema = "navigation";

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();

    public NavigationDbContext(DbContextOptions<NavigationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NavigationDbContext).Assembly);
    }
}

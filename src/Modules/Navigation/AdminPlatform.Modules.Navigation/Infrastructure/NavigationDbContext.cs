using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

public sealed class NavigationDbContext : DbContext, INavigationDbContext
{
    public const string Schema = "navigation";

    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();
    public DbSet<NavigationItemSiteDetail> NavigationItemSiteDetails => Set<NavigationItemSiteDetail>();
    public DbSet<NavigationItemPermission> NavigationItemPermissions => Set<NavigationItemPermission>();

    public NavigationDbContext(DbContextOptions<NavigationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NavigationDbContext).Assembly);
    }
}

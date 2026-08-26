using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

/// <summary>Idempotent base admin sidebar: a Dashboard entry visible to everyone, and an Administration
/// group whose children are gated by the matching *.view permission code from each module. Upserted by
/// Code — safe to run on every deploy.</summary>
public static class NavigationSeeder
{
    private sealed record MenuSeed(string Code, string Name, string? ParentCode, string? Route, string? Icon, int SortOrder, string? PermissionCode);

    private static readonly MenuSeed[] Menus =
    [
        new("dashboard", "Dashboard", null, "/dashboard", "home", 0, null),
        new("admin", "Administration", null, null, "settings", 100, null),
        new("admin.users", "Users", "admin", "/admin/users", "users", 10, "users.view"),
        new("admin.roles", "Roles", "admin", "/admin/roles", "shield", 20, "roles.view"),
        new("admin.permissions", "Permissions", "admin", "/admin/permissions", "key", 30, "permissions.view"),
        new("admin.organizations", "Organizations", "admin", "/admin/organizations", "building", 40, "organizations.view"),
        new("admin.departments", "Departments", "admin", "/admin/departments", "sitemap", 50, "departments.view"),
        new("admin.brands", "Brands", "admin", "/admin/brands", "tag", 60, "brands.view"),
        new("admin.menus", "Menus", "admin", "/admin/menus", "menu", 70, "menus.view"),
        new("admin.fiscal-years", "Fiscal Years", "admin", "/admin/fiscal-years", "calendar", 80, "fiscal-years.view"),
        new("admin.system-settings", "System Settings", "admin", "/admin/system-settings", "sliders", 90, "system-settings.view"),
        new("admin.audit-logs", "Audit Logs", "admin", "/admin/audit-logs", "history", 100, "audit-logs.view"),
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<INavigationDbContext>();

        var existing = await db.Menus.ToDictionaryAsync(m => m.Code, cancellationToken);

        foreach (var seed in Menus)
        {
            if (existing.ContainsKey(seed.Code))
            {
                continue;
            }

            Guid? parentId = seed.ParentCode is not null && existing.TryGetValue(seed.ParentCode, out var parent) ? parent.Id : null;
            var menu = Menu.Create(seed.Code, seed.Name, parentId, seed.Route, seed.Icon, seed.SortOrder);
            db.Menus.Add(menu);
            existing[seed.Code] = menu;
        }

        await db.SaveChangesAsync(cancellationToken);

        var existingLinks = await db.MenuPermissions.ToListAsync(cancellationToken);
        foreach (var seed in Menus)
        {
            if (seed.PermissionCode is null)
            {
                continue;
            }

            var menu = existing[seed.Code];
            var alreadyLinked = existingLinks.Any(l => l.MenuId == menu.Id && l.PermissionCode == seed.PermissionCode);
            if (!alreadyLinked)
            {
                db.MenuPermissions.Add(MenuPermission.Create(menu.Id, seed.PermissionCode));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

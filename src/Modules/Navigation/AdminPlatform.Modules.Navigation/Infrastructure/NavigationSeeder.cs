using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

/// <summary>Idempotent base admin sidebar — the single source of truth for the admin portal's navigation
/// (the frontend renders GET /api/v1/navigation/menus as-is). Routes match the frontend's app/admin pages
/// and icons are lucide-react component names from the frontend's navigation icon registry.
///
/// Root entries WITH a route are standalone links; root entries WITHOUT a route are section headings
/// whose children are the links (a heading with no visible child is hidden per caller). Entries gated by a
/// permission code are only shown to callers holding it; entries without one (domains that do not have a
/// backend module yet) are shown to every admin.
///
/// Runs on every deploy: missing entries are created; for entries this seeder owns, the structural fields
/// (parent, route, icon, sort order) are re-synced so a frontend route change ships with the seed. The
/// active flag is never overwritten, and a name only when it still is a previous seed default — both are
/// editable in Admin → Hệ thống → Menu quản trị.</summary>
public static class NavigationSeeder
{
    private sealed record MenuSeed(string Code, string Name, string? ParentCode, string? Route, string? Icon, int SortOrder, string? PermissionCode);

    private static readonly MenuSeed[] Menus =
    [
        new("dashboard", "Dashboard", null, "/admin", "LayoutDashboard", 1, null),
        new("admin.users", "Người dùng", null, "/admin/users", "Users", 2, "users.view"),
        new("admin.roles", "Vai trò", null, "/admin/roles", "ShieldCheck", 3, "roles.view"),
        new("admin.permissions", "Quyền", null, "/admin/permissions", "KeyRound", 4, "permissions.view"),

        new("sales", "Sales", null, null, null, 10, null),
        new("sales.orders", "Đơn hàng", "sales", "/admin/sales/orders", "ClipboardList", 1, null),
        new("sales.customers", "Khách hàng", "sales", "/admin/sales/customers", "Contact", 2, "customers.view"),
        new("sales.payments", "Thanh toán", "sales", "/admin/sales/payments", "CreditCard", 3, null),

        new("catalog", "Catalog", null, null, null, 20, null),
        new("catalog.categories", "Danh mục", "catalog", "/admin/catalog/categories", "FolderTree", 1, "categories.view"),
        new("admin.media", "Media", "catalog", "/admin/catalog/media", "Images", 2, "media.view"),
        new("catalog.products", "Sản phẩm", "catalog", "/admin/catalog/products", "Package", 3, "products.view"),
        new("catalog.menus", "Thực đơn", "catalog", "/admin/catalog/menus", "BookOpen", 4, "sales-menus.view"),
        new("catalog.menu-products", "Liên kết Menu-SP", "catalog", "/admin/catalog/menu-products", "ListChecks", 5, "sales-menus.view"),
        new("catalog.modifier-groups", "Tùy chọn món (Modifier)", "catalog", "/admin/catalog/modifier-groups", "Tags", 6, "modifier-groups.view"),
        new("catalog.promotions", "Mã giảm giá", "catalog", "/admin/catalog/promotions", "TicketPercent", 7, "promotions.view"),

        new("content", "Content", null, null, null, 30, null),
        new("content.pages", "Page", "content", "/admin/content/pages", "FileText", 1, "pages.view"),
        new("content.banners", "Banner", "content", "/admin/content/banners", "GalleryHorizontal", 2, "banners.view"),
        new("content.articles", "Bài viết", "content", "/admin/content/articles", "Newspaper", 3, "articles.view"),
        new("content.article-categories", "Danh mục bài viết", "content", "/admin/content/article-categories", "FolderTree", 4, "article-categories.view"),
        new("content.article-tags", "Thẻ bài viết", "content", "/admin/content/article-tags", "Tags", 5, "article-tags.view"),

        new("brand", "Brand", null, null, null, 40, null),
        new("brand.settings", "Cài đặt thương hiệu", "brand", "/admin/brand/settings", "Store", 1, null),

        new("seo", "SEO", null, null, null, 50, null),
        new("seo.dashboard", "Tổng quan", "seo", "/admin/seo", "Gauge", 1, null),
        new("seo.metadata", "SEO Metadata", "seo", "/admin/seo/metadata", "Search", 2, null),
        new("seo.settings", "Cài đặt SEO", "seo", "/admin/seo/settings", "Settings2", 3, null),
        new("seo.redirects", "Chuyển hướng (Redirects)", "seo", "/admin/seo/redirects", "ArrowRightLeft", 4, null),

        new("organization", "Tổ chức", null, null, null, 60, null),
        new("admin.organizations", "Tổ chức", "organization", "/admin/organization/organizations", "Building2", 1, "organizations.view"),
        new("admin.departments", "Phòng ban", "organization", "/admin/organization/departments", "Network", 2, "departments.view"),
        new("admin.brands", "Chi nhánh / Brand", "organization", "/admin/organization/brands", "BadgeCheck", 3, "brands.view"),

        new("system", "Hệ thống", null, null, null, 70, null),
        new("admin.menus", "Menu quản trị", "system", "/admin/system/menus", "PanelLeft", 1, "menus.view"),
        new("admin.fiscal-years", "Năm tài chính", "system", "/admin/system/fiscal-years", "CalendarRange", 2, "fiscal-years.view"),
        new("admin.system-settings", "Cài đặt hệ thống", "system", "/admin/system/settings", "SlidersHorizontal", 3, "system-settings.view"),
        new("admin.audit-logs", "Nhật ký thay đổi", "system", "/admin/system/audit-logs", "History", 4, "audit-logs.view"),

        new("config", "Cấu hình", null, null, null, 80, null),
        new("config.payment-methods", "Phương thức thanh toán", "config", "/admin/settings/payment-methods", "Wallet", 1, null),
        new("config.delivery-methods", "Phương thức giao hàng", "config", "/admin/settings/delivery-methods", "Truck", 2, null),
        new("config.navigation", "Navigation website", "config", "/admin/settings/navigation", "Route", 3, null),
        new("config.order-options", "Tùy chọn chung đơn hàng", "config", "/admin/settings/order-options", "Utensils", 4, null),
    ];

    /// <summary>English names an earlier version of this seeder used. An entry still carrying its old
    /// default name was never renamed by an admin, so it is safe to move it to the current name.</summary>
    private static readonly Dictionary<string, string> PreviousDefaultNames = new()
    {
        ["admin.users"] = "Users",
        ["admin.roles"] = "Roles",
        ["admin.permissions"] = "Permissions",
        ["admin.organizations"] = "Organizations",
        ["admin.departments"] = "Departments",
        ["admin.brands"] = "Brands",
        ["admin.menus"] = "Menus",
        ["admin.fiscal-years"] = "Fiscal Years",
        ["admin.system-settings"] = "System Settings",
        ["admin.audit-logs"] = "Audit Logs",
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<INavigationDbContext>();

        var existing = await db.Menus.ToDictionaryAsync(m => m.Code, cancellationToken);

        // Parents are declared before their children, so a single pass can resolve every parent id.
        foreach (var seed in Menus)
        {
            Guid? parentId = seed.ParentCode is null ? null : existing[seed.ParentCode].Id;

            if (existing.TryGetValue(seed.Code, out var menu))
            {
                var name = PreviousDefaultNames.TryGetValue(seed.Code, out var previousName) && menu.Name == previousName
                    ? seed.Name
                    : menu.Name;
                menu.Update(name, menu.IsActive, parentId, seed.Route, seed.Icon, seed.SortOrder);
            }
            else
            {
                menu = Menu.Create(seed.Code, seed.Name, parentId, seed.Route, seed.Icon, seed.SortOrder);
                db.Menus.Add(menu);
                existing[seed.Code] = menu;
            }

            // Saved per entry: a new parent must be persisted before a child can reference its id.
            await db.SaveChangesAsync(cancellationToken);
        }

        var existingLinks = await db.MenuPermissions.ToListAsync(cancellationToken);
        foreach (var seed in Menus.Where(s => s.PermissionCode is not null))
        {
            var menu = existing[seed.Code];
            if (!existingLinks.Any(l => l.MenuId == menu.Id && l.PermissionCode == seed.PermissionCode))
            {
                db.MenuPermissions.Add(MenuPermission.Create(menu.Id, seed.PermissionCode!));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

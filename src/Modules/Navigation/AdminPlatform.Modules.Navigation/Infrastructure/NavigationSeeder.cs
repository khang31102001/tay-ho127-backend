using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Navigation.Infrastructure;

/// <summary>Idempotent seed of the navigation fixtures.
///
/// ADMIN sidebar — the single source of truth for the admin portal's navigation (the frontend renders
/// GET /api/v1/navigation/me as-is). Routes match the frontend's app/admin pages and icons are lucide-react
/// component names from the frontend's icon registry. Items without a route are group headings whose children are
/// the links (a heading with no visible child is hidden per caller). Items gated by a permission code are only shown
/// to callers holding it; items without one (domains that do not have a backend module yet) are shown to every
/// admin. Runs on every deploy: missing items are created; for items this seeder owns the structural fields (parent,
/// group flag, route, icon, sort order) are re-synced so a frontend route change ships with the seed. The active
/// flag is never overwritten, and a name only when it still is a previous seed default.
///
/// WEBSITE menus (header / footer / mobile) — editorial content, so they are seeded only into a menu that has no
/// items yet; once an editor owns the menu the seed never touches it again.</summary>
public static class NavigationSeeder
{
    public const string AdminSidebarCode = "admin-sidebar";

    private sealed record MenuSeed(string Code, string Name, NavigationScope Scope, NavigationLocation Location);

    private sealed record SiteTarget(NavigationTargetType Type, bool OpenInNewTab = false);

    private sealed record ItemSeed(
        string MenuCode, string Code, string Label, string? ParentCode, string? Url, string? Icon, int SortOrder,
        string? PermissionCode = null, SiteTarget? Site = null)
    {
        public bool IsGroup => Url is null && Site is null;
    }

    private static readonly MenuSeed[] Menus =
    [
        new(AdminSidebarCode, "Menu quản trị", NavigationScope.Admin, NavigationLocation.Sidebar),
        new("site-header", "Website Header", NavigationScope.Site, NavigationLocation.Header),
        new("site-footer", "Website Footer", NavigationScope.Site, NavigationLocation.Footer),
        new("site-mobile", "Mobile Navigation", NavigationScope.Site, NavigationLocation.Mobile),
    ];

    private static ItemSeed Admin(string code, string label, string? parent, string? url, string? icon, int sort, string? permission) =>
        new(AdminSidebarCode, code, label, parent, url, icon, sort, permission);

    private static ItemSeed Link(string menu, string code, string label, string? parent, string url, int sort, SiteTarget? target = null) =>
        new(menu, code, label, parent, url, null, sort, null, target ?? new SiteTarget(NavigationTargetType.Route));

    private static readonly ItemSeed[] AdminItems =
    [
        Admin("dashboard", "Dashboard", null, "/admin", "LayoutDashboard", 1, null),
        Admin("admin.users", "Người dùng", null, "/admin/users", "Users", 2, "users.view"),
        Admin("admin.roles", "Vai trò", null, "/admin/roles", "ShieldCheck", 3, "roles.view"),
        Admin("admin.permissions", "Quyền", null, "/admin/permissions", "KeyRound", 4, "permissions.view"),

        Admin("sales", "Sales", null, null, null, 10, null),
        Admin("sales.orders", "Đơn hàng", "sales", "/admin/sales/orders", "ClipboardList", 1, "orders.view"),
        Admin("sales.customers", "Khách hàng", "sales", "/admin/sales/customers", "Contact", 2, "customers.view"),
        Admin("sales.payments", "Thanh toán", "sales", "/admin/sales/payments", "CreditCard", 3, "payments.view"),

        Admin("catalog", "Catalog", null, null, null, 20, null),
        Admin("catalog.categories", "Danh mục", "catalog", "/admin/catalog/categories", "FolderTree", 1, "categories.view"),
        Admin("admin.media", "Media", "catalog", "/admin/catalog/media", "Images", 2, "media.view"),
        Admin("catalog.products", "Sản phẩm", "catalog", "/admin/catalog/products", "Package", 3, "products.view"),
        Admin("catalog.menus", "Thực đơn", "catalog", "/admin/catalog/menus", "BookOpen", 4, "sales-menus.view"),
        Admin("catalog.menu-products", "Liên kết Menu-SP", "catalog", "/admin/catalog/menu-products", "ListChecks", 5, "sales-menus.view"),
        Admin("catalog.modifier-groups", "Tùy chọn món (Modifier)", "catalog", "/admin/catalog/modifier-groups", "Tags", 6, "modifier-groups.view"),
        Admin("catalog.promotions", "Mã giảm giá", "catalog", "/admin/catalog/promotions", "TicketPercent", 7, "promotions.view"),

        Admin("content", "Content", null, null, null, 30, null),
        Admin("content.pages", "Page", "content", "/admin/content/pages", "FileText", 1, "pages.view"),
        Admin("content.banners", "Banner", "content", "/admin/content/banners", "GalleryHorizontal", 2, "banners.view"),
        Admin("content.articles", "Bài viết", "content", "/admin/content/articles", "Newspaper", 3, "articles.view"),
        Admin("content.article-categories", "Danh mục bài viết", "content", "/admin/content/article-categories", "FolderTree", 4, "article-categories.view"),
        Admin("content.article-tags", "Thẻ bài viết", "content", "/admin/content/article-tags", "Tags", 5, "article-tags.view"),

        Admin("seo", "SEO", null, null, null, 50, null),
        Admin("seo.dashboard", "Tổng quan", "seo", "/admin/seo", "Gauge", 1, null),
        Admin("seo.metadata", "SEO Metadata", "seo", "/admin/seo/metadata", "Search", 2, "seo-metadata.view"),
        Admin("seo.settings", "Cài đặt SEO", "seo", "/admin/seo/settings", "Settings2", 3, "seo-settings.view"),
        Admin("seo.redirects", "Chuyển hướng (Redirects)", "seo", "/admin/seo/redirects", "ArrowRightLeft", 4, "redirects.view"),

        Admin("organization", "Tổ chức", null, null, null, 60, null),
        Admin("admin.organizations", "Tổ chức", "organization", "/admin/organization/organizations", "Building2", 1, "organizations.view"),
        Admin("admin.departments", "Phòng ban", "organization", "/admin/organization/departments", "Network", 2, "departments.view"),
        Admin("admin.brand-profile", "Thông tin thương hiệu", "organization", "/admin/organization/brand-profile", "Store", 0, "brand-profile.view"),
        Admin("admin.brands", "Chi nhánh", "organization", "/admin/organization/brands", "BadgeCheck", 3, "brands.view"),

        Admin("system", "Hệ thống", null, null, null, 70, null),
        Admin("admin.menus", "Menu quản trị", "system", "/admin/system/menus", "PanelLeft", 1, "menus.view"),
        Admin("admin.fiscal-years", "Năm tài chính", "system", "/admin/system/fiscal-years", "CalendarRange", 2, "fiscal-years.view"),
        Admin("admin.system-settings", "Cài đặt hệ thống", "system", "/admin/system/settings", "SlidersHorizontal", 3, "system-settings.view"),
        Admin("admin.audit-logs", "Nhật ký thay đổi", "system", "/admin/system/audit-logs", "History", 4, "audit-logs.view"),

        Admin("config", "Cấu hình", null, null, null, 80, null),
        Admin("config.payment-methods", "Phương thức thanh toán", "config", "/admin/settings/payment-methods", "Wallet", 1, "payment-methods.view"),
        Admin("config.delivery-methods", "Phương thức giao hàng", "config", "/admin/settings/delivery-methods", "Truck", 2, "delivery-methods.view"),
        Admin("config.navigation", "Navigation website", "config", "/admin/settings/navigation", "Route", 3, "site-navigation.view"),
        Admin("config.order-options", "Tùy chọn chung đơn hàng", "config", "/admin/settings/order-options", "Utensils", 4, "order-option-groups.view"),
        Admin("config.order-settings", "Cấu hình đơn hàng", "config", "/admin/settings/order-settings", "Settings2", 5, "order-settings.view"),
    ];

    // Header and mobile share the same three links; the footer adds a nested "Thực đơn" branch (3 levels) and the
    // Facebook page. Only links that exist on the website are seeded.
    private static readonly ItemSeed[] SiteItems =
    [
        Link("site-header", "header.home", "Trang chủ", null, "/", 1),
        Link("site-header", "header.menu", "Thực đơn", null, "/thuc-don", 2),
        Link("site-header", "header.news", "Tin tức", null, "/tin-tuc", 3),

        Link("site-mobile", "mobile.home", "Trang chủ", null, "/", 1),
        Link("site-mobile", "mobile.menu", "Thực đơn", null, "/thuc-don", 2),
        Link("site-mobile", "mobile.news", "Tin tức", null, "/tin-tuc", 3),

        Link("site-footer", "footer.home", "Trang chủ", null, "/", 1),
        Link("site-footer", "footer.menu", "Thực đơn", null, "/thuc-don", 2),
        Link("site-footer", "footer.menu.banh-cuon", "Bánh cuốn", "footer.menu", "/thuc-don", 1),
        Link("site-footer", "footer.menu.them", "Món thêm", "footer.menu", "/thuc-don", 2),
        Link("site-footer", "footer.menu.them.cha", "Chả", "footer.menu.them", "/thuc-don", 1),
        Link("site-footer", "footer.menu.them.nem", "Nem", "footer.menu.them", "/thuc-don", 2),
        Link("site-footer", "footer.menu.do-uong", "Đồ uống", "footer.menu", "/thuc-don", 3),
        Link("site-footer", "footer.news", "Tin tức", null, "/tin-tuc", 3),
        Link("site-footer", "footer.facebook", "Theo dõi Facebook", null, "https://www.facebook.com/tayho127", 5,
            new SiteTarget(NavigationTargetType.External, OpenInNewTab: true)),
    ];

    /// <summary>Entries that no longer exist in the sidebar (the old "Brand → Cài đặt thương hiệu" group became
    /// "Tổ chức → Thông tin thương hiệu"). Switched off rather than deleted, so an admin can still see them.</summary>
    private static readonly string[] RetiredAdminItemCodes = ["brand.settings", "brand"];

    /// <summary>English names an earlier version of this seeder used. An item still carrying its old default name was
    /// never renamed by an admin, so it is safe to move it to the current name.</summary>
    private static readonly Dictionary<string, string> PreviousDefaultNames = new()
    {
        ["admin.users"] = "Users",
        ["admin.roles"] = "Roles",
        ["admin.permissions"] = "Permissions",
        ["admin.organizations"] = "Organizations",
        ["admin.departments"] = "Departments",
        ["admin.brands"] = "Chi nhánh / Brand",
        ["admin.menus"] = "Menus",
        ["admin.fiscal-years"] = "Fiscal Years",
        ["admin.system-settings"] = "System Settings",
        ["admin.audit-logs"] = "Audit Logs",
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<INavigationDbContext>();

        var menusByCode = await db.NavigationMenus.ToDictionaryAsync(m => m.Code, cancellationToken);
        foreach (var seed in Menus)
        {
            if (!menusByCode.ContainsKey(seed.Code))
            {
                var menu = NavigationMenu.Create(seed.Code, seed.Name, seed.Scope, seed.Location);
                db.NavigationMenus.Add(menu);
                menusByCode[seed.Code] = menu;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await SeedAdminSidebarAsync(db, menusByCode[AdminSidebarCode], cancellationToken);

        foreach (var menu in Menus.Where(m => m.Scope == NavigationScope.Site).Select(m => menusByCode[m.Code]))
        {
            await SeedSiteMenuIfEmptyAsync(db, menu, cancellationToken);
        }
    }

    private static async Task SeedAdminSidebarAsync(INavigationDbContext db, NavigationMenu menu, CancellationToken cancellationToken)
    {
        var existing = await db.NavigationItems.Where(i => i.MenuId == menu.Id).ToDictionaryAsync(i => i.Code, cancellationToken);

        // Parents are declared before their children, so a single pass can resolve every parent id.
        foreach (var seed in AdminItems)
        {
            Guid? parentId = seed.ParentCode is null ? null : existing[seed.ParentCode].Id;

            if (existing.TryGetValue(seed.Code, out var item))
            {
                var label = PreviousDefaultNames.TryGetValue(seed.Code, out var previousName) && item.Name == previousName
                    ? seed.Label
                    : item.Name;
                item.Update(label, item.IsActive, parentId, seed.IsGroup, seed.Url, seed.Icon, seed.SortOrder);
            }
            else
            {
                item = NavigationItem.Create(menu.Id, seed.Code, seed.Label, parentId, seed.IsGroup, seed.Url, seed.Icon, seed.SortOrder);
                db.NavigationItems.Add(item);
                existing[seed.Code] = item;
            }

            // Saved per item: a new parent must be persisted before a child can reference its id.
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var code in RetiredAdminItemCodes)
        {
            if (existing.TryGetValue(code, out var retired) && retired.IsActive)
            {
                retired.Update(retired.Name, false, retired.ParentId, retired.IsGroup, retired.Url, retired.Icon, retired.SortOrder);
            }
        }

        var existingLinks = await db.NavigationItemPermissions.ToListAsync(cancellationToken);
        foreach (var seed in AdminItems.Where(s => s.PermissionCode is not null))
        {
            var item = existing[seed.Code];
            if (!existingLinks.Any(l => l.ItemId == item.Id && l.PermissionCode == seed.PermissionCode))
            {
                db.NavigationItemPermissions.Add(NavigationItemPermission.Create(item.Id, seed.PermissionCode!));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSiteMenuIfEmptyAsync(INavigationDbContext db, NavigationMenu menu, CancellationToken cancellationToken)
    {
        if (await db.NavigationItems.AnyAsync(i => i.MenuId == menu.Id, cancellationToken))
        {
            return;
        }

        var created = new Dictionary<string, NavigationItem>();
        foreach (var seed in SiteItems.Where(s => s.MenuCode == menu.Code))
        {
            Guid? parentId = seed.ParentCode is null ? null : created[seed.ParentCode].Id;
            var item = NavigationItem.Create(menu.Id, seed.Code, seed.Label, parentId, seed.IsGroup, seed.Url, seed.Icon, seed.SortOrder);
            db.NavigationItems.Add(item);
            created[seed.Code] = item;

            if (seed.Site is { } site)
            {
                db.NavigationItemSiteDetails.Add(NavigationItemSiteDetail.Create(item.Id, site.Type, null, site.OpenInNewTab));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

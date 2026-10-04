namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>The seeded permission hierarchy, three levels: Module → Resource group → permission (leaf).
/// Modules and resource groups mirror the admin sidebar. It only names resources (strings); the leaf codes
/// themselves still come from each module's own *Permissions.All, assembled by the Migrator.</summary>
public static class DefaultPermissionTree
{
    public const string FallbackGroupCode = "group:other";

    public static PermissionTreeSeed Value { get; } = new(
        Groups:
        [
            new("group:access", "Người dùng & phân quyền", null, 10),
            new("group:access.users", "Người dùng", "group:access", 1),
            new("group:access.roles", "Vai trò", "group:access", 2),
            new("group:access.permissions", "Quyền", "group:access", 3),

            new("group:organization", "Tổ chức", null, 20),
            new("group:organization.organizations", "Tổ chức", "group:organization", 1),
            new("group:organization.departments", "Phòng ban", "group:organization", 2),
            new("group:organization.brand-profile", "Thông tin thương hiệu", "group:organization", 3),
            new("group:organization.brands", "Chi nhánh", "group:organization", 4),

            new("group:catalog", "Catalog", null, 30),
            new("group:catalog.categories", "Danh mục", "group:catalog", 1),
            new("group:catalog.media", "Media", "group:catalog", 2),
            new("group:catalog.products", "Sản phẩm", "group:catalog", 3),
            new("group:catalog.sales-menus", "Thực đơn", "group:catalog", 4),
            new("group:catalog.modifier-groups", "Tùy chọn món (Modifier)", "group:catalog", 5),
            new("group:catalog.promotions", "Mã giảm giá", "group:catalog", 6),

            new("group:content", "Nội dung", null, 40),
            new("group:content.pages", "Page", "group:content", 1),
            new("group:content.banners", "Banner", "group:content", 2),
            new("group:content.articles", "Bài viết", "group:content", 3),
            new("group:content.article-categories", "Danh mục bài viết", "group:content", 4),
            new("group:content.article-tags", "Thẻ bài viết", "group:content", 5),

            new("group:sales", "Bán hàng", null, 50),
            new("group:sales.orders", "Đơn hàng", "group:sales", 1),
            new("group:sales.customers", "Khách hàng", "group:sales", 2),
            new("group:sales.payments", "Thanh toán", "group:sales", 3),

            new("group:seo", "SEO", null, 60),
            new("group:seo.metadata", "SEO Metadata", "group:seo", 1),
            new("group:seo.schemas", "Schema / JSON-LD", "group:seo", 2),
            new("group:seo.settings", "Cài đặt SEO", "group:seo", 3),
            new("group:seo.redirects", "Chuyển hướng (Redirects)", "group:seo", 4),

            new("group:config", "Cấu hình bán hàng", null, 70),
            new("group:config.payment-methods", "Phương thức thanh toán", "group:config", 1),
            new("group:config.delivery-methods", "Phương thức giao hàng", "group:config", 2),
            new("group:config.order-options", "Tùy chọn chung đơn hàng", "group:config", 3),
            new("group:config.order-settings", "Cấu hình đơn hàng", "group:config", 4),

            new("group:system", "Hệ thống", null, 80),
            new("group:system.menus", "Menu quản trị", "group:system", 1),
            new("group:system.fiscal-years", "Năm tài chính", "group:system", 2),
            new("group:system.settings", "Cài đặt hệ thống", "group:system", 3),
            new("group:system.audit-logs", "Nhật ký thay đổi", "group:system", 4),
            new("group:system.site-navigation", "Menu website", "group:system", 5),

            new(FallbackGroupCode, "Khác", null, 99),
        ],
        GroupCodeByResource: new Dictionary<string, string>
        {
            ["users"] = "group:access.users",
            ["roles"] = "group:access.roles",
            ["permissions"] = "group:access.permissions",

            ["organizations"] = "group:organization.organizations",
            ["departments"] = "group:organization.departments",
            ["brand-profile"] = "group:organization.brand-profile",
            ["brands"] = "group:organization.brands",

            ["categories"] = "group:catalog.categories",
            ["media"] = "group:catalog.media",
            ["products"] = "group:catalog.products",
            ["sales-menus"] = "group:catalog.sales-menus",
            ["modifier-groups"] = "group:catalog.modifier-groups",
            ["promotions"] = "group:catalog.promotions",

            ["pages"] = "group:content.pages",
            ["banners"] = "group:content.banners",
            ["articles"] = "group:content.articles",
            ["article-categories"] = "group:content.article-categories",
            ["article-tags"] = "group:content.article-tags",

            ["orders"] = "group:sales.orders",
            ["customers"] = "group:sales.customers",
            ["payments"] = "group:sales.payments",

            ["seo-metadata"] = "group:seo.metadata",
            ["seo-schemas"] = "group:seo.schemas",
            ["seo-settings"] = "group:seo.settings",
            ["redirects"] = "group:seo.redirects",

            ["payment-methods"] = "group:config.payment-methods",
            ["delivery-methods"] = "group:config.delivery-methods",
            ["order-option-groups"] = "group:config.order-options",
            ["order-settings"] = "group:config.order-settings",

            ["menus"] = "group:system.menus",
            ["fiscal-years"] = "group:system.fiscal-years",
            ["system-settings"] = "group:system.settings",
            ["audit-logs"] = "group:system.audit-logs",
            ["site-navigation"] = "group:system.site-navigation",
        },
        FallbackGroupCode: FallbackGroupCode);
}

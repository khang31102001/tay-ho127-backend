# Dữ liệu mẫu: Permission (cây quyền) + Navigation (Admin + Website)

Mục đích: bản kê **dữ liệu chuẩn** của 2 nghiệp vụ nền tảng. Sau khi thiết kế xong, dùng file này để
(1) kiểm tra seeder tạo đúng, (2) chạy lại `migrate` + `seed` trên DB trống để test, không cần backup/restore.
Nguồn sự thật trong code: `DefaultPermissionTree.cs`, `*Permissions.All`, `NavigationSeeder.cs`.
Thiết kế: `fontend/docs/proposals/permission-navigation-foundation.md`.

Trạng thái: **Phần 1 (Permission) — đã triển khai**. **Phần 2 (Navigation) — dữ liệu chuẩn bị sẵn, chưa triển khai (Bước B).**

## Cách nạp lại toàn bộ dữ liệu mẫu (DB test/dev)

```bash
# từ thư mục backend, ConnectionStrings__Default trỏ tới DB TEST (không phải DB production)
dotnet run --project src/Tools/AdminPlatform.Migrator -- migrate
dotnet run --project src/Tools/AdminPlatform.Migrator -- seed          # catalog quyền + cây nhóm + sidebar
dotnet run --project src/Tools/AdminPlatform.Migrator -- seed-demo     # tùy chọn: role demo-manager/staff/viewer
# idempotent: chạy lại lần 2 không được tạo thêm dòng nào (kiểm tra bằng các truy vấn ở cuối file)
```

---

## PHẦN 1 — Cây quyền (đã triển khai)

Quy ước: **Module (cấp 1) → Nhóm tài nguyên (cấp 2) → Quyền lá (cấp 3)**.
Nhóm: `is_group = true`, code `group:*`, không vào `role_permissions`/JWT. Lá: code `resource.action` (không đổi so với trước).
Khóa gán nhóm cho lá: phần trước dấu `.` đầu tiên của code lá (`users.roles.manage` → resource `users`).
Lá có resource chưa khai báo → nhóm `group:other` (Khác).

| Cấp 1 — Module (sort) | Cấp 2 — Nhóm tài nguyên | Quyền lá (thứ tự = thứ tự catalog) |
|---|---|---|
| `group:access` Người dùng & phân quyền (10) | `group:access.users` Người dùng | users.view, users.create, users.update, users.reset-password, users.roles.manage, users.departments.manage, users.brands.manage |
| | `group:access.roles` Vai trò | roles.view, roles.create, roles.update, roles.delete, roles.permissions.manage |
| | `group:access.permissions` Quyền | permissions.view, permissions.create, permissions.update, permissions.delete |
| `group:organization` Tổ chức (20) | `group:organization.organizations` Tổ chức | organizations.view, .create, .update |
| | `group:organization.departments` Phòng ban | departments.view, .create, .update |
| | `group:organization.brand-profile` Thông tin thương hiệu | brand-profile.view, brand-profile.update |
| | `group:organization.brands` Chi nhánh | brands.view, .create, .update |
| `group:catalog` Catalog (30) | `group:catalog.categories` Danh mục | categories.view/create/update/delete |
| | `group:catalog.media` Media | media.view/create/update/delete |
| | `group:catalog.products` Sản phẩm | products.view/create/update/delete |
| | `group:catalog.sales-menus` Thực đơn | sales-menus.view/create/update/delete |
| | `group:catalog.modifier-groups` Tùy chọn món | modifier-groups.view/create/update/delete |
| | `group:catalog.promotions` Mã giảm giá | promotions.view/create/update/delete |
| `group:content` Nội dung (40) | `group:content.pages` Page | pages.view/create/update/delete |
| | `group:content.banners` Banner | banners.view/create/update/delete |
| | `group:content.articles` Bài viết | articles.view/create/update/delete |
| | `group:content.article-categories` Danh mục bài viết | article-categories.view/create/update/delete |
| | `group:content.article-tags` Thẻ bài viết | article-tags.view/create/update/delete |
| `group:sales` Bán hàng (50) | `group:sales.orders` Đơn hàng | orders.view, orders.update-status, orders.cancel |
| | `group:sales.customers` Khách hàng | customers.view, .create, .update, customers.addresses.manage |
| | `group:sales.payments` Thanh toán | payments.view, payments.manage |
| `group:seo` SEO (60) | `group:seo.metadata` SEO Metadata | seo-metadata.view/update/delete |
| | `group:seo.schemas` Schema / JSON-LD | seo-schemas.view/update/delete |
| | `group:seo.settings` Cài đặt SEO | seo-settings.view, seo-settings.update |
| | `group:seo.redirects` Chuyển hướng | redirects.view/create/update/delete |
| `group:config` Cấu hình bán hàng (70) | `group:config.payment-methods` Phương thức thanh toán | payment-methods.view/create/update/delete |
| | `group:config.delivery-methods` Phương thức giao hàng | delivery-methods.view/create/update/delete |
| | `group:config.order-options` Tùy chọn chung đơn hàng | order-option-groups.view/create/update/delete |
| | `group:config.order-settings` Cấu hình đơn hàng | order-settings.view, order-settings.update |
| `group:system` Hệ thống (80) | `group:system.menus` Menu quản trị | menus.view/create/update/delete, menus.permissions.manage |
| | `group:system.fiscal-years` Năm tài chính | fiscal-years.view/create/update |
| | `group:system.settings` Cài đặt hệ thống | system-settings.view/create/update/delete |
| | `group:system.audit-logs` Nhật ký thay đổi | audit-logs.view |
| `group:other` Khác (99) | — | (lá chưa khai báo resource) |

Số liệu kỳ vọng sau `seed` trên DB trống: **42 nhóm** (8 module + 33 nhóm tài nguyên + 1 "Khác"),
**119 quyền lá** (đếm từ các `*Permissions.All`), `group:other` rỗng; mọi lá có `parent_id` khác NULL; `super-admin` có đủ **mọi lá, không có nhóm**.
Khi Bước B thêm `site-navigation.view|create|update|delete`, bổ sung resource `site-navigation` → nhóm
`group:system.site-navigation` ("Menu website") và cập nhật bảng này.

Role demo (`seed-demo`): `demo-manager`, `demo-staff`, `demo-viewer` — chỉ được gán **lá** (đã lọc `!is_group`).

---

## PHẦN 2 — Navigation (dữ liệu chuẩn bị cho Bước B)

### Container (`navigation_menus`)

| code | name | scope | location |
|---|---|---|---|
| `admin-sidebar` | Menu quản trị | ADMIN | sidebar |
| `site-header` | Website Header | SITE | header |
| `site-footer` | Website Footer | SITE | footer |
| `site-mobile` | Mobile Navigation | SITE | mobile |

Giới hạn độ sâu menu SITE: **3 cấp**. Mỗi `(scope, location)` đúng 1 menu.

### Item ADMIN — `admin-sidebar` (nguồn: `NavigationSeeder.cs`, hiện tại)
`is_group = true` khi không có route. Quyền = `navigation_item_permissions` (any-of).

| code | label | parent | url | icon | sort | quyền |
|---|---|---|---|---|---|---|
| dashboard | Dashboard | — | /admin | LayoutDashboard | 1 | — |
| admin.users | Người dùng | — | /admin/users | Users | 2 | users.view |
| admin.roles | Vai trò | — | /admin/roles | ShieldCheck | 3 | roles.view |
| admin.permissions | Quyền | — | /admin/permissions | KeyRound | 4 | permissions.view |
| sales *(group)* | Sales | — | — | — | 10 | — |
| sales.orders | Đơn hàng | sales | /admin/sales/orders | ClipboardList | 1 | orders.view |
| sales.customers | Khách hàng | sales | /admin/sales/customers | Contact | 2 | customers.view |
| sales.payments | Thanh toán | sales | /admin/sales/payments | CreditCard | 3 | payments.view |
| catalog *(group)* | Catalog | — | — | — | 20 | — |
| catalog.categories | Danh mục | catalog | /admin/catalog/categories | FolderTree | 1 | categories.view |
| admin.media | Media | catalog | /admin/catalog/media | Images | 2 | media.view |
| catalog.products | Sản phẩm | catalog | /admin/catalog/products | Package | 3 | products.view |
| catalog.menus | Thực đơn | catalog | /admin/catalog/menus | BookOpen | 4 | sales-menus.view |
| catalog.menu-products | Liên kết Menu-SP | catalog | /admin/catalog/menu-products | ListChecks | 5 | sales-menus.view |
| catalog.modifier-groups | Tùy chọn món (Modifier) | catalog | /admin/catalog/modifier-groups | Tags | 6 | modifier-groups.view |
| catalog.promotions | Mã giảm giá | catalog | /admin/catalog/promotions | TicketPercent | 7 | promotions.view |
| content *(group)* | Content | — | — | — | 30 | — |
| content.pages | Page | content | /admin/content/pages | FileText | 1 | pages.view |
| content.banners | Banner | content | /admin/content/banners | GalleryHorizontal | 2 | banners.view |
| content.articles | Bài viết | content | /admin/content/articles | Newspaper | 3 | articles.view |
| content.article-categories | Danh mục bài viết | content | /admin/content/article-categories | FolderTree | 4 | article-categories.view |
| content.article-tags | Thẻ bài viết | content | /admin/content/article-tags | Tags | 5 | article-tags.view |
| seo *(group)* | SEO | — | — | — | 50 | — |
| seo.dashboard | Tổng quan | seo | /admin/seo | Gauge | 1 | — |
| seo.metadata | SEO Metadata | seo | /admin/seo/metadata | Search | 2 | seo-metadata.view |
| seo.settings | Cài đặt SEO | seo | /admin/seo/settings | Settings2 | 3 | seo-settings.view |
| seo.redirects | Chuyển hướng (Redirects) | seo | /admin/seo/redirects | ArrowRightLeft | 4 | redirects.view |
| organization *(group)* | Tổ chức | — | — | — | 60 | — |
| admin.brand-profile | Thông tin thương hiệu | organization | /admin/organization/brand-profile | Store | 0 | brand-profile.view |
| admin.organizations | Tổ chức | organization | /admin/organization/organizations | Building2 | 1 | organizations.view |
| admin.departments | Phòng ban | organization | /admin/organization/departments | Network | 2 | departments.view |
| admin.brands | Chi nhánh | organization | /admin/organization/brands | BadgeCheck | 3 | brands.view |
| system *(group)* | Hệ thống | — | — | — | 70 | — |
| admin.menus | Menu quản trị | system | /admin/system/menus | PanelLeft | 1 | menus.view |
| admin.fiscal-years | Năm tài chính | system | /admin/system/fiscal-years | CalendarRange | 2 | fiscal-years.view |
| admin.system-settings | Cài đặt hệ thống | system | /admin/system/settings | SlidersHorizontal | 3 | system-settings.view |
| admin.audit-logs | Nhật ký thay đổi | system | /admin/system/audit-logs | History | 4 | audit-logs.view |
| config *(group)* | Cấu hình | — | — | — | 80 | — |
| config.payment-methods | Phương thức thanh toán | config | /admin/settings/payment-methods | Wallet | 1 | payment-methods.view |
| config.delivery-methods | Phương thức giao hàng | config | /admin/settings/delivery-methods | Truck | 2 | delivery-methods.view |
| config.navigation | Navigation website | config | /admin/settings/navigation | Route | 3 | *(Bước B: site-navigation.view)* |
| config.order-options | Tùy chọn chung đơn hàng | config | /admin/settings/order-options | Utensils | 4 | order-option-groups.view |
| config.order-settings | Cấu hình đơn hàng | config | /admin/settings/order-settings | Settings2 | 5 | order-settings.view |

Mục đã ngừng (seed tắt `is_active`, không xóa): `brand`, `brand.settings`.

### Item SITE — nguồn: `features/navigation/mocks/navigation.mock.ts` (sẽ bị xóa ở Bước B)
Ghi chú: mock đang trỏ Thực đơn về `/menu` (redirect 301 sang `/thuc-don`) → **seed dùng `/thuc-don`**.
`target_type`: ROUTE = đường dẫn nội bộ, PAGE = trỏ Page CMS (URL lấy theo Page đã xuất bản), EXTERNAL = URL ngoài.

**`site-header`**

| code | label | parent | target | url | sort |
|---|---|---|---|---|---|
| header.home | Trang chủ | — | ROUTE | / | 1 |
| header.menu | Thực đơn | — | ROUTE | /thuc-don | 2 |
| header.news | Tin tức | — | ROUTE | /tin-tuc | 3 |

**`site-mobile`** — cùng 3 mục như header (code `mobile.home|menu|news`, sort 1–3).

**`site-footer`**

| code | label | parent | target | url | sort |
|---|---|---|---|---|---|
| footer.home | Trang chủ | — | ROUTE | / | 1 |
| footer.menu | Thực đơn | — | ROUTE | /thuc-don | 2 |
| footer.menu.banh-cuon | Bánh cuốn | footer.menu | ROUTE | /thuc-don | 1 |
| footer.menu.them | Món thêm *(nhóm con)* | footer.menu | ROUTE | /thuc-don | 2 |
| footer.menu.them.cha | Chả | footer.menu.them | ROUTE | /thuc-don | 1 |
| footer.menu.them.nem | Nem | footer.menu.them | ROUTE | /thuc-don | 2 |
| footer.menu.do-uong | Đồ uống | footer.menu | ROUTE | /thuc-don | 3 |
| footer.news | Tin tức | — | ROUTE | /tin-tuc | 3 |
| footer.contact | Liên hệ | — | PAGE (target_id = Page "lien-he" nếu có; nếu chưa có thì dùng url dự phòng) | /lien-he | 4 |
| footer.facebook | Theo dõi Facebook | — | EXTERNAL, mở tab mới | https://www.facebook.com/tayho127 *(link Facebook thật cần chủ site xác nhận — xem plan, mục 5)* | 5 |

Footer có 3 cấp (Thực đơn → Món thêm → Chả/Nem) — đúng giới hạn tối đa của menu SITE.

---

## Truy vấn kiểm tra sau seed (Postgres)

```sql
-- Cây quyền
SELECT count(*) FILTER (WHERE is_group) AS groups,
       count(*) FILTER (WHERE NOT is_group) AS leaves,
       count(*) FILTER (WHERE NOT is_group AND parent_id IS NULL) AS orphan_leaves   -- kỳ vọng 0
FROM access_control.permissions;

-- Không nhóm nào bị gán cho vai trò
SELECT count(*) FROM access_control.role_permissions rp
JOIN access_control.permissions p ON p.id = rp.permission_id WHERE p.is_group;       -- kỳ vọng 0

-- super-admin đủ mọi lá
SELECT (SELECT count(*) FROM access_control.permissions WHERE NOT is_group) AS leaves,
       (SELECT count(*) FROM access_control.role_permissions rp
          JOIN access_control.roles r ON r.id = rp.role_id AND r.code = 'super-admin') AS granted;  -- bằng nhau
```

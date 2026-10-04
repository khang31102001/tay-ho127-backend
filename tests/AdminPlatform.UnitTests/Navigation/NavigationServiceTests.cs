using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Navigation.Application;
using AdminPlatform.Modules.Navigation.Application.Containers;
using AdminPlatform.Modules.Navigation.Application.Items;
using AdminPlatform.Modules.Navigation.Application.MyNavigation;
using AdminPlatform.Modules.Navigation.Application.Public;
using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.Modules.Navigation.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Navigation;

public class NavigationServiceTests
{
    private sealed class FakeUser(params string[] permissions) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public string? Email => null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions { get; } = permissions;
        public Guid? CurrentBrandId => null;
        public Guid? CurrentFiscalYearId => null;
    }

    private static readonly string[] AllNavigationPermissions =
    [
        "menus.view", "menus.create", "menus.update", "menus.delete", "menus.permissions.manage",
        "site-navigation.view", "site-navigation.create", "site-navigation.update", "site-navigation.delete",
    ];

    private static NavigationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<NavigationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static NavigationItemService NewItemService(INavigationDbContext db, ICurrentUser? user = null, bool allowSitePermissions = false) =>
        new(db, user ?? new FakeUser(AllNavigationPermissions), Options.Create(new NavigationOptions { AllowSitePermissions = allowSitePermissions }));

    private static async Task<(NavigationMenu Admin, NavigationMenu Header, NavigationMenu Footer)> AddMenusAsync(INavigationDbContext db)
    {
        var admin = NavigationMenu.Create("admin-sidebar", "Sidebar", NavigationScope.Admin, NavigationLocation.Sidebar);
        var header = NavigationMenu.Create("site-header", "Header", NavigationScope.Site, NavigationLocation.Header);
        var footer = NavigationMenu.Create("site-footer", "Footer", NavigationScope.Site, NavigationLocation.Footer);
        db.NavigationMenus.AddRange(admin, header, footer);
        await db.SaveChangesAsync(CancellationToken.None);
        return (admin, header, footer);
    }

    private static CreateNavigationItemRequest SiteRoute(Guid menuId, string label, Guid? parentId = null, string url = "/thuc-don", int sort = 1) =>
        new(menuId, null, label, parentId, false, url, null, sort, new NavigationSiteDetailRequest("route", null, false));

    private static CreateNavigationItemRequest AdminLink(Guid menuId, string label, Guid? parentId = null, int sort = 1) =>
        new(menuId, null, label, parentId, false, "/admin/x", "Star", sort, null);

    private static CreateNavigationItemRequest Group(Guid menuId, string label, Guid? parentId = null, int sort = 1) =>
        new(menuId, null, label, parentId, true, null, null, sort, null);

    // ---------------------------------------------------------------- shape rules

    [Fact]
    public async Task Create_a_website_link_needs_a_target_and_the_url_must_match_its_type()
    {
        var db = NewDb();
        var (_, header, _) = await AddMenusAsync(db);
        var sut = NewItemService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(header.Id, null, "No target", null, false, "/x", null, 1, null), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            SiteRoute(header.Id, "Bad route", url: "thuc-don"), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(header.Id, null, "Bad external", null, false, "ftp://x", null, 1, new("external", null, false)), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(header.Id, null, "Page without id", null, false, null, null, 1, new("page", null, false)), CancellationToken.None));

        var ok = await sut.CreateAsync(SiteRoute(header.Id, "Thực đơn"), CancellationToken.None);
        Assert.Equal("route", ok.Site!.TargetType);
        var external = await sut.CreateAsync(
            new(header.Id, null, "Facebook", null, false, "https://facebook.com/x", null, 2, new("external", null, true)), CancellationToken.None);
        Assert.True(external.Site!.OpenInNewTab);
        var page = await sut.CreateAsync(
            new(header.Id, null, "Liên hệ", null, false, "/lien-he", null, 3, new("page", Guid.NewGuid(), false)), CancellationToken.None);
        Assert.NotNull(page.Site!.TargetId);
    }

    [Fact]
    public async Task Admin_items_have_no_website_target_and_need_a_route_and_a_group_has_no_link()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var sut = NewItemService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(admin.Id, null, "With site", null, false, "/admin/x", null, 1, new("route", null, false)), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(admin.Id, null, "No route", null, false, null, null, 1, null), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new(header.Id, null, "Group with url", null, true, "/x", null, 1, null), CancellationToken.None));

        var group = await sut.CreateAsync(Group(admin.Id, "Section"), CancellationToken.None);
        Assert.True(group.IsGroup);
        Assert.Null(group.Url);
        Assert.Null(group.Site);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_code_within_a_menu_but_allows_it_in_another_menu()
    {
        var db = NewDb();
        var (_, header, footer) = await AddMenusAsync(db);
        var sut = NewItemService(db);

        await sut.CreateAsync(SiteRoute(header.Id, "Home") with { Code = "home" }, CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(
            SiteRoute(header.Id, "Home 2") with { Code = "home" }, CancellationToken.None));
        await sut.CreateAsync(SiteRoute(footer.Id, "Home") with { Code = "home" }, CancellationToken.None);
    }

    // ---------------------------------------------------------------- tree rules

    [Fact]
    public async Task A_menu_cannot_be_deeper_than_three_levels_and_the_parent_must_be_in_the_same_menu()
    {
        var db = NewDb();
        var (_, header, footer) = await AddMenusAsync(db);
        var sut = NewItemService(db);
        var level1 = await sut.CreateAsync(SiteRoute(header.Id, "L1"), CancellationToken.None);
        var level2 = await sut.CreateAsync(SiteRoute(header.Id, "L2", level1.Id), CancellationToken.None);
        var level3 = await sut.CreateAsync(SiteRoute(header.Id, "L3", level2.Id), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            SiteRoute(header.Id, "L4", level3.Id), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            SiteRoute(footer.Id, "Foreign parent", level1.Id), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            SiteRoute(header.Id, "Missing parent", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Update_rejects_moving_an_item_under_its_own_descendant_or_past_the_depth_limit()
    {
        var db = NewDb();
        var (_, header, _) = await AddMenusAsync(db);
        var sut = NewItemService(db);
        var root = await sut.CreateAsync(SiteRoute(header.Id, "Root"), CancellationToken.None);
        var child = await sut.CreateAsync(SiteRoute(header.Id, "Child", root.Id), CancellationToken.None);
        var other = await sut.CreateAsync(SiteRoute(header.Id, "Other", sort: 2), CancellationToken.None);
        var otherChild = await sut.CreateAsync(SiteRoute(header.Id, "OtherChild", other.Id), CancellationToken.None);
        var otherGrandchild = await sut.CreateAsync(SiteRoute(header.Id, "OtherGrandchild", otherChild.Id), CancellationToken.None);

        UpdateNavigationItemRequest Move(NavigationItemResponse item, Guid? parentId) =>
            new(item.Label, true, parentId, false, item.Url, null, item.SortOrder, new("route", null, false));

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(root.Id, Move(root, child.Id), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(root.Id, Move(root, root.Id), CancellationToken.None));
        // root has height 2; moving it below a level-3 item would make 5 levels.
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(root.Id, Move(root, otherGrandchild.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Reorder_renumbers_the_items_under_the_new_parent_and_validates_the_move()
    {
        var db = NewDb();
        var (_, header, footer) = await AddMenusAsync(db);
        var sut = NewItemService(db);
        var a = await sut.CreateAsync(SiteRoute(header.Id, "A", sort: 1), CancellationToken.None);
        var b = await sut.CreateAsync(SiteRoute(header.Id, "B", sort: 2), CancellationToken.None);
        var c = await sut.CreateAsync(SiteRoute(header.Id, "C", sort: 3), CancellationToken.None);
        var foreign = await sut.CreateAsync(SiteRoute(footer.Id, "Foreign"), CancellationToken.None);

        await sut.ReorderAsync(new(header.Id, null, [c.Id, a.Id, b.Id]), CancellationToken.None);
        var listed = await sut.ListAsync(header.Id, new(), CancellationToken.None);
        Assert.Equal(["C", "A", "B"], listed.Items.Select(i => i.Label));
        Assert.Equal([1, 2, 3], listed.Items.Select(i => i.SortOrder));

        await sut.ReorderAsync(new(header.Id, a.Id, [b.Id, c.Id]), CancellationToken.None);
        var moved = await sut.GetByIdAsync(b.Id, CancellationToken.None);
        Assert.Equal(a.Id, moved.ParentId);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ReorderAsync(new(header.Id, null, [foreign.Id]), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.ReorderAsync(new(header.Id, b.Id, [a.Id]), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_refuses_an_item_with_children_and_removes_its_details_and_permission_links()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var sut = NewItemService(db);
        var parent = await sut.CreateAsync(SiteRoute(header.Id, "Parent"), CancellationToken.None);
        var child = await sut.CreateAsync(SiteRoute(header.Id, "Child", parent.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => sut.DeleteAsync(parent.Id, CancellationToken.None));
        await sut.DeleteAsync(child.Id, CancellationToken.None);
        await sut.DeleteAsync(parent.Id, CancellationToken.None);
        Assert.Empty(await db.NavigationItemSiteDetails.ToListAsync());

        var adminItem = await sut.CreateAsync(AdminLink(admin.Id, "Admin item"), CancellationToken.None);
        await sut.SetPermissionsAsync(adminItem.Id, new(["users.view"]), CancellationToken.None);
        await sut.DeleteAsync(adminItem.Id, CancellationToken.None);
        Assert.Empty(await db.NavigationItemPermissions.ToListAsync());
    }

    // ---------------------------------------------------------------- permissions & authorization

    [Fact]
    public async Task Website_items_cannot_be_gated_until_it_is_enabled_and_groups_never_gate()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var siteItem = await NewItemService(db).CreateAsync(SiteRoute(header.Id, "Home"), CancellationToken.None);
        var adminItem = await NewItemService(db).CreateAsync(AdminLink(admin.Id, "Users"), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => NewItemService(db).SetPermissionsAsync(siteItem.Id, new(["users.view"]), CancellationToken.None));
        // Clearing is always allowed.
        await NewItemService(db).SetPermissionsAsync(siteItem.Id, new([]), CancellationToken.None);

        await NewItemService(db, allowSitePermissions: true).SetPermissionsAsync(siteItem.Id, new(["users.view"]), CancellationToken.None);
        Assert.Equal(["users.view"], await NewItemService(db).GetPermissionCodesAsync(siteItem.Id, CancellationToken.None));

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => NewItemService(db).SetPermissionsAsync(adminItem.Id, new(["group:access.users"]), CancellationToken.None));

        await NewItemService(db).SetPermissionsAsync(adminItem.Id, new(["users.view", "users.view", "roles.view"]), CancellationToken.None);
        Assert.Equal(2, (await NewItemService(db).GetPermissionCodesAsync(adminItem.Id, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task The_permission_that_applies_depends_on_the_scope_of_the_menu()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var editor = NewItemService(db, new FakeUser("site-navigation.view", "site-navigation.create"));
        var sidebarAdmin = NewItemService(db, new FakeUser("menus.view", "menus.create"));
        var nobody = NewItemService(db, new FakeUser());

        await editor.CreateAsync(SiteRoute(header.Id, "Home"), CancellationToken.None);
        await Assert.ThrowsAsync<ForbiddenException>(() => editor.CreateAsync(AdminLink(admin.Id, "Hack"), CancellationToken.None));

        await sidebarAdmin.CreateAsync(AdminLink(admin.Id, "Users"), CancellationToken.None);
        await Assert.ThrowsAsync<ForbiddenException>(() => sidebarAdmin.CreateAsync(SiteRoute(header.Id, "Nope"), CancellationToken.None));

        await Assert.ThrowsAsync<ForbiddenException>(() => nobody.ListAsync(null, new(), CancellationToken.None));

        // Listing everything only returns the scopes the caller may view.
        var editorList = await editor.ListAsync(null, new(), CancellationToken.None);
        Assert.All(editorList.Items, i => Assert.Equal(header.Id, i.MenuId));
        var sidebarList = await sidebarAdmin.ListAsync(null, new(), CancellationToken.None);
        Assert.All(sidebarList.Items, i => Assert.Equal(admin.Id, i.MenuId));
        await Assert.ThrowsAsync<ForbiddenException>(() => editor.ListAsync(admin.Id, new(), CancellationToken.None));
    }

    [Fact]
    public async Task Containers_list_only_viewable_scopes_and_allow_editing_just_name_and_active_flag()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var editor = new NavigationMenuService(db, new FakeUser("site-navigation.view", "site-navigation.update"));

        var listed = await editor.ListAsync(CancellationToken.None);
        Assert.All(listed, m => Assert.Equal("site", m.Scope));

        var updated = await editor.UpdateAsync(header.Id, new("Renamed", false), CancellationToken.None);
        Assert.Equal("Renamed", updated.Name);
        Assert.False(updated.IsActive);
        Assert.Equal("header", updated.Location);

        await Assert.ThrowsAsync<ForbiddenException>(() => editor.UpdateAsync(admin.Id, new("X", true), CancellationToken.None));
    }

    // ---------------------------------------------------------------- the admin sidebar

    [Fact]
    public async Task The_sidebar_is_filtered_by_permission_never_shows_website_menus_and_hides_empty_headings()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var items = NewItemService(db);
        var dashboard = await items.CreateAsync(AdminLink(admin.Id, "Dashboard", sort: 1), CancellationToken.None);
        var users = await items.CreateAsync(AdminLink(admin.Id, "Users", sort: 2), CancellationToken.None);
        var section = await items.CreateAsync(Group(admin.Id, "Section", sort: 3), CancellationToken.None);
        var orders = await items.CreateAsync(AdminLink(admin.Id, "Orders", section.Id), CancellationToken.None);
        var emptySection = await items.CreateAsync(Group(admin.Id, "Empty", sort: 4), CancellationToken.None);
        await items.CreateAsync(SiteRoute(header.Id, "Website link"), CancellationToken.None);
        await items.SetPermissionsAsync(users.Id, new(["users.view"]), CancellationToken.None);
        await items.SetPermissionsAsync(orders.Id, new(["orders.view"]), CancellationToken.None);
        var sut = new MyNavigationService(db);

        var staff = await sut.GetVisibleMenuTreeAsync(["orders.view"], CancellationToken.None);
        Assert.Equal(["Dashboard", "Section"], staff.Select(n => n.Name));
        Assert.Equal(["Orders"], staff.Single(n => n.Name == "Section").Children.Select(c => c.Name));
        Assert.DoesNotContain(staff, n => n.Id == emptySection.Id);

        var nobody = await sut.GetVisibleMenuTreeAsync([], CancellationToken.None);
        Assert.Equal([dashboard.Id], nobody.Select(n => n.Id));

        var all = await sut.GetVisibleMenuTreeAsync(["users.view", "orders.view"], CancellationToken.None);
        Assert.Equal(["Dashboard", "Users", "Section"], all.Select(n => n.Name));
        Assert.DoesNotContain(all.SelectMany(n => n.Children).Concat(all), n => n.Name == "Website link");
    }

    // ---------------------------------------------------------------- the public website menus

    [Fact]
    public async Task The_public_menu_returns_the_sorted_tree_and_hides_inactive_gated_and_admin_items()
    {
        var db = NewDb();
        var (admin, header, _) = await AddMenusAsync(db);
        var items = NewItemService(db, allowSitePermissions: true);
        await items.CreateAsync(SiteRoute(header.Id, "Home", sort: 2, url: "/"), CancellationToken.None);
        var menu = await items.CreateAsync(SiteRoute(header.Id, "Menu", sort: 1), CancellationToken.None);
        await items.CreateAsync(SiteRoute(header.Id, "Sub", menu.Id), CancellationToken.None);
        var hidden = await items.CreateAsync(SiteRoute(header.Id, "Hidden", sort: 3), CancellationToken.None);
        var hiddenChild = await items.CreateAsync(SiteRoute(header.Id, "Under hidden", hidden.Id), CancellationToken.None);
        var gated = await items.CreateAsync(SiteRoute(header.Id, "Gated", sort: 4), CancellationToken.None);
        var emptyGroup = await items.CreateAsync(Group(header.Id, "Empty group", sort: 5), CancellationToken.None);
        await items.CreateAsync(AdminLink(admin.Id, "Admin only"), CancellationToken.None);

        await items.UpdateAsync(hidden.Id, new(hidden.Label, false, null, false, hidden.Url, null, 3, new("route", null, false)), CancellationToken.None);
        await items.SetPermissionsAsync(gated.Id, new(["users.view"]), CancellationToken.None);
        var sut = new PublicNavigationService(db);

        var result = await sut.GetByLocationAsync("HEADER", CancellationToken.None);

        Assert.Equal("header", result.Location);
        Assert.Equal(["Menu", "Home"], result.Items.Select(i => i.Label));
        Assert.Equal(["Sub"], result.Items[0].Children.Select(c => c.Label));
        Assert.DoesNotContain(result.Items, i => i.Id == hidden.Id || i.Id == gated.Id || i.Id == emptyGroup.Id);
        Assert.All(result.Items, i => Assert.Equal("route", i.TargetType));
        Assert.DoesNotContain(result.Items.SelectMany(i => i.Children), c => c.Id == hiddenChild.Id);
    }

    [Fact]
    public async Task The_public_menu_is_empty_when_switched_off_and_rejects_unknown_or_admin_locations()
    {
        var db = NewDb();
        var (_, header, _) = await AddMenusAsync(db);
        await NewItemService(db).CreateAsync(SiteRoute(header.Id, "Home"), CancellationToken.None);
        var sut = new PublicNavigationService(db);

        Assert.Single((await sut.GetByLocationAsync("header", CancellationToken.None)).Items);

        header.Update(header.Name, false);
        await db.SaveChangesAsync(CancellationToken.None);
        Assert.Empty((await sut.GetByLocationAsync("header", CancellationToken.None)).Items);

        // No mobile menu exists at all: still a normal, empty answer.
        Assert.Empty((await sut.GetByLocationAsync("mobile", CancellationToken.None)).Items);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByLocationAsync("sidebar", CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByLocationAsync("nope", CancellationToken.None));
    }

    // ---------------------------------------------------------------- seeder

    [Fact]
    public async Task The_seeder_creates_the_four_menus_once_and_seeds_website_items_only_into_empty_menus()
    {
        var db = NewDb();
        var services = new ServiceCollection().AddSingleton<INavigationDbContext>(db).BuildServiceProvider();

        await NavigationSeeder.SeedAsync(services, CancellationToken.None);

        var menus = await db.NavigationMenus.ToListAsync();
        Assert.Equal(4, menus.Count);
        var adminMenu = menus.Single(m => m.Scope == NavigationScope.Admin);
        Assert.Equal(NavigationSeeder.AdminSidebarCode, adminMenu.Code);

        var adminItems = await db.NavigationItems.Where(i => i.MenuId == adminMenu.Id).ToListAsync();
        Assert.All(adminItems.Where(i => i.IsGroup), i => Assert.Null(i.Url));
        Assert.All(adminItems.Where(i => !i.IsGroup), i => Assert.StartsWith("/admin", i.Url));
        var navigationItem = adminItems.Single(i => i.Code == "config.navigation");
        var links = await db.NavigationItemPermissions.Where(p => p.ItemId == navigationItem.Id).Select(p => p.PermissionCode).ToListAsync();
        Assert.Equal(["site-navigation.view"], links);

        var footer = menus.Single(m => m.Location == NavigationLocation.Footer);
        var footerItems = await db.NavigationItems.Where(i => i.MenuId == footer.Id).ToListAsync();
        Assert.Equal(9, footerItems.Count);
        Assert.Equal(3, footerItems.Max(i => NavigationTreeRules.Depth(i.Id, footerItems.ToDictionary(x => x.Id, x => x.ParentId))));
        var facebook = footerItems.Single(i => i.Code == "footer.facebook");
        var detail = await db.NavigationItemSiteDetails.SingleAsync(d => d.ItemId == facebook.Id);
        Assert.Equal(NavigationTargetType.External, detail.TargetType);
        Assert.True(detail.OpenInNewTab);

        // An editor changes the footer; re-seeding must neither duplicate nor restore anything.
        db.NavigationItems.Remove(footerItems.Single(i => i.Code == "footer.news"));
        await db.SaveChangesAsync();
        var countBeforeReseed = await db.NavigationItems.CountAsync();
        await NavigationSeeder.SeedAsync(services, CancellationToken.None);

        Assert.Equal(countBeforeReseed, await db.NavigationItems.CountAsync());
        Assert.DoesNotContain(await db.NavigationItems.ToListAsync(), i => i.Code == "footer.news");
        Assert.Equal(4, await db.NavigationMenus.CountAsync());
    }

    [Fact]
    public async Task The_seeder_keeps_admin_edits_to_the_active_flag_and_names_but_resyncs_structure()
    {
        var db = NewDb();
        var services = new ServiceCollection().AddSingleton<INavigationDbContext>(db).BuildServiceProvider();
        await NavigationSeeder.SeedAsync(services, CancellationToken.None);

        var users = await db.NavigationItems.SingleAsync(i => i.Code == "admin.users");
        users.Update("Tài khoản", false, null, false, "/old-route", "Old", 99);
        await db.SaveChangesAsync();

        await NavigationSeeder.SeedAsync(services, CancellationToken.None);

        users = await db.NavigationItems.SingleAsync(i => i.Code == "admin.users");
        Assert.Equal("Tài khoản", users.Name);
        Assert.False(users.IsActive);
        Assert.Equal("/admin/users", users.Url);
        Assert.Equal(2, users.SortOrder);
        Assert.Equal(1, await db.NavigationItems.CountAsync(i => i.Code == "admin.users"));
    }

    // ---------------------------------------------------------------- pure rules

    [Fact]
    public void NavigationTreeRules_compute_depth_height_and_descendants()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [a] = null, [b] = a, [c] = b };

        Assert.Equal(3, NavigationTreeRules.Depth(c, parents));
        Assert.Equal(3, NavigationTreeRules.SubtreeHeight(a, parents));
        Assert.Equal(1, NavigationTreeRules.SubtreeHeight(c, parents));
        Assert.True(NavigationTreeRules.IsSelfOrDescendant(c, a, parents));
        Assert.True(NavigationTreeRules.IsSelfOrDescendant(a, a, parents));
        Assert.False(NavigationTreeRules.IsSelfOrDescendant(a, c, parents));
    }

    [Fact]
    public void NavigationMenu_only_accepts_valid_scope_and_location_pairs()
    {
        Assert.Throws<BusinessRuleValidationException>(() => NavigationMenu.Create("x", "X", NavigationScope.Admin, NavigationLocation.Header));
        Assert.Throws<BusinessRuleValidationException>(() => NavigationMenu.Create("x", "X", NavigationScope.Site, NavigationLocation.Sidebar));
        Assert.Equal(NavigationLocation.Footer, NavigationMenu.Create("x", "X", NavigationScope.Site, NavigationLocation.Footer).Location);
    }
}

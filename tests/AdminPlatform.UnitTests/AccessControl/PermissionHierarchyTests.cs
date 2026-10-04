using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Application.Permissions;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using AdminPlatform.Modules.AccessControl.Domain;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.UnitTests.AccessControl;

public class PermissionHierarchyTests
{
    private static AccessControlDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Permission> AddGroupAsync(IAccessControlDbContext db, string code, Guid? parentId = null)
    {
        var group = Permission.CreateGroup(code, code, parentId, 0);
        db.Permissions.Add(group);
        await db.SaveChangesAsync(CancellationToken.None);
        return group;
    }

    private static async Task<Permission> AddLeafAsync(IAccessControlDbContext db, string code, Guid parentId, bool isActive = true)
    {
        var leaf = Permission.Create(code, code, parentId, 0);
        if (!isActive)
        {
            leaf.Update(code, false);
        }

        db.Permissions.Add(leaf);
        await db.SaveChangesAsync(CancellationToken.None);
        return leaf;
    }

    [Fact]
    public async Task SetPermissionsAsync_expands_a_group_to_its_active_leaves_at_every_depth_and_stores_only_leaves()
    {
        var db = NewDb();
        var module = await AddGroupAsync(db, "group:catalog");
        var resource = await AddGroupAsync(db, "group:catalog.products", module.Id);
        var view = await AddLeafAsync(db, "products.view", resource.Id);
        var update = await AddLeafAsync(db, "products.update", resource.Id);
        var inactive = await AddLeafAsync(db, "products.legacy", resource.Id, isActive: false);
        var elsewhere = await AddLeafAsync(db, "orders.view", (await AddGroupAsync(db, "group:sales")).Id);
        var roles = new RoleService(db);
        var role = await roles.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);

        await roles.SetPermissionsAsync(role.Id, new AssignPermissionsRequest([module.Id]), CancellationToken.None);

        var granted = (await roles.GetPermissionIdsAsync(role.Id, CancellationToken.None)).ToHashSet();
        Assert.Equal(new[] { view.Id, update.Id }.ToHashSet(), granted);
        Assert.DoesNotContain(inactive.Id, granted);
        Assert.DoesNotContain(elsewhere.Id, granted);
        Assert.DoesNotContain(module.Id, granted);
    }

    [Fact]
    public async Task SetPermissionsAsync_merges_explicit_leaves_with_a_selected_group()
    {
        var db = NewDb();
        var group = await AddGroupAsync(db, "group:catalog");
        var inGroup = await AddLeafAsync(db, "products.view", group.Id);
        var outside = await AddLeafAsync(db, "orders.view", (await AddGroupAsync(db, "group:sales")).Id);
        var roles = new RoleService(db);
        var role = await roles.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);

        await roles.SetPermissionsAsync(role.Id, new AssignPermissionsRequest([group.Id, outside.Id]), CancellationToken.None);

        var granted = (await roles.GetPermissionIdsAsync(role.Id, CancellationToken.None)).ToHashSet();
        Assert.Equal(new[] { inGroup.Id, outside.Id }.ToHashSet(), granted);
    }

    [Fact]
    public async Task GetTreeAsync_nests_by_sort_order_and_drops_an_inactive_group_with_its_subtree()
    {
        var db = NewDb();
        var module = await AddGroupAsync(db, "group:catalog");
        var resource = await AddGroupAsync(db, "group:catalog.products", module.Id);
        await AddLeafAsync(db, "products.view", resource.Id);
        var hidden = await AddGroupAsync(db, "group:hidden");
        await AddLeafAsync(db, "hidden.view", hidden.Id);
        hidden.Update(hidden.Name, false);
        await db.SaveChangesAsync(CancellationToken.None);
        var sut = new PermissionService(db);

        var tree = await sut.GetTreeAsync(CancellationToken.None);

        var root = Assert.Single(tree);
        Assert.Equal("group:catalog", root.Code);
        var resourceNode = Assert.Single(root.Children);
        Assert.Equal("products.view", Assert.Single(resourceNode.Children).Code);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_leaf_parent_and_a_missing_parent()
    {
        var db = NewDb();
        var group = await AddGroupAsync(db, "group:catalog");
        var leaf = await AddLeafAsync(db, "products.view", group.Id);
        var sut = new PermissionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new CreatePermissionRequest("products.create", "Create", leaf.Id, false, 0), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new CreatePermissionRequest("products.create", "Create", Guid.NewGuid(), false, 0), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_group_deeper_than_the_maximum()
    {
        var db = NewDb();
        Guid? parentId = null;
        for (var level = 1; level <= Permission.MaxDepth; level++)
        {
            parentId = (await AddGroupAsync(db, $"group:level-{level}", parentId)).Id;
        }

        var sut = new PermissionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(
            new CreatePermissionRequest("group:too-deep", "Too deep", parentId, true, 0), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_rejects_a_move_under_a_descendant_and_a_move_that_exceeds_the_depth()
    {
        var db = NewDb();
        var top = await AddGroupAsync(db, "group:top");
        var child = await AddGroupAsync(db, "group:top.child", top.Id);
        Guid? deepParent = null;
        for (var level = 1; level <= Permission.MaxDepth - 1; level++)
        {
            deepParent = (await AddGroupAsync(db, $"group:deep-{level}", deepParent)).Id;
        }

        var sut = new PermissionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(
            top.Id, new UpdatePermissionRequest(top.Name, true, child.Id, true, 0), CancellationToken.None));
        // top has height 2; its new parent chain is already 4 deep → 6 > 5.
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(
            top.Id, new UpdatePermissionRequest(top.Name, true, deepParent, true, 0), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_only_allows_a_group_to_become_a_leaf_when_it_has_no_children()
    {
        var db = NewDb();
        var group = await AddGroupAsync(db, "group:catalog");
        await AddLeafAsync(db, "products.view", group.Id);
        var parent = await AddGroupAsync(db, "group:other");
        var sut = new PermissionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.UpdateAsync(
            group.Id, new UpdatePermissionRequest(group.Name, true, parent.Id, false, 0), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_refuses_a_group_that_still_has_children()
    {
        var db = NewDb();
        var group = await AddGroupAsync(db, "group:catalog");
        await AddLeafAsync(db, "products.view", group.Id);
        var sut = new PermissionService(db);

        await Assert.ThrowsAsync<ConflictException>(() => sut.DeleteAsync(group.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RolePermissionQueryService_never_returns_a_group_code_even_if_a_role_holds_one()
    {
        var db = NewDb();
        var group = await AddGroupAsync(db, "group:catalog");
        var leaf = await AddLeafAsync(db, "products.view", group.Id);
        var role = Role.Create("editor", "Editor");
        db.Roles.Add(role);
        var userId = Guid.NewGuid();
        db.UserRoles.Add(UserRole.Create(userId, role.Id));
        db.RolePermissions.Add(RolePermission.Create(role.Id, group.Id));
        db.RolePermissions.Add(RolePermission.Create(role.Id, leaf.Id));
        await db.SaveChangesAsync(CancellationToken.None);

        // The service is internal to the module; resolve it the way DI would, by type.
        var serviceType = typeof(AccessControlSeeder).Assembly
            .GetType("AdminPlatform.Modules.AccessControl.Infrastructure.RolePermissionQueryService")!;
        var query = (IRolePermissionQueryService)Activator.CreateInstance(serviceType, db)!;
        var snapshot = await query.GetForUserAsync(userId, CancellationToken.None);

        Assert.Equal(["products.view"], snapshot.Permissions);
    }

    [Fact]
    public async Task SeedAsync_builds_the_tree_grants_leaves_to_super_admin_and_is_idempotent()
    {
        var db = NewDb();
        var services = new ServiceCollection().AddSingleton<IAccessControlDbContext>(db).BuildServiceProvider();
        var catalog = new (string Code, string Description)[]
        {
            ("products.view", "View products"),
            ("products.create", "Create products"),
            ("brand-new.view", "Unmapped resource"),
        };
        var tree = new PermissionTreeSeed(
            [
                new("group:catalog", "Catalog", null, 1),
                new("group:catalog.products", "Products", "group:catalog", 1),
                new("group:other", "Other", null, 99),
            ],
            new Dictionary<string, string> { ["products"] = "group:catalog.products" },
            "group:other");

        await AccessControlSeeder.SeedAsync(services, catalog, null, CancellationToken.None, tree);
        await AccessControlSeeder.SeedAsync(services, catalog, null, CancellationToken.None, tree);

        var all = await db.Permissions.ToListAsync();
        Assert.Equal(6, all.Count);
        var byCode = all.ToDictionary(p => p.Code);
        Assert.Equal(byCode["group:catalog.products"].Id, byCode["products.view"].ParentId);
        Assert.Equal(byCode["group:other"].Id, byCode["brand-new.view"].ParentId);
        Assert.Equal(byCode["group:catalog"].Id, byCode["group:catalog.products"].ParentId);
        Assert.True(byCode["products.view"].SortOrder < byCode["products.create"].SortOrder);

        var superAdmin = await db.Roles.SingleAsync(r => r.Code == AccessControlSeeder.SuperAdminRoleCode);
        var granted = await db.RolePermissions.Where(rp => rp.RoleId == superAdmin.Id).Select(rp => rp.PermissionId).ToListAsync();
        Assert.Equal(3, granted.Count);
        Assert.All(granted, id => Assert.False(all.Single(p => p.Id == id).IsGroup));
    }

    [Fact]
    public void PermissionTreeRules_compute_depth_height_and_descendants()
    {
        var a = new PermissionNodeInfo(Guid.NewGuid(), null, true, true);
        var b = new PermissionNodeInfo(Guid.NewGuid(), a.Id, true, true);
        var c = new PermissionNodeInfo(Guid.NewGuid(), b.Id, false, true);
        var byId = new[] { a, b, c }.ToDictionary(n => n.Id);
        var children = new[] { a, b, c }.Where(n => n.ParentId is not null).ToLookup(n => n.ParentId!.Value);

        Assert.Equal(3, PermissionTreeRules.Depth(c.Id, byId));
        Assert.Equal(3, PermissionTreeRules.SubtreeHeight(a.Id, children));
        Assert.True(PermissionTreeRules.IsSelfOrDescendant(c.Id, a.Id, byId));
        Assert.False(PermissionTreeRules.IsSelfOrDescendant(a.Id, c.Id, byId));
        Assert.Equal([c.Id], PermissionTreeRules.ActiveLeavesUnder([a.Id], byId.Values.ToList()));
    }
}

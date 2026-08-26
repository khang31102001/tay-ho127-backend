using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Application.Roles;
using AdminPlatform.Modules.AccessControl.Domain;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.AccessControl;

public class RoleServiceTests
{
    private static IAccessControlDbContext NewDb() =>
        new AccessControlDbContext(new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_code()
    {
        var db = NewDb();
        var sut = new RoleService(db);
        await sut.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.CreateAsync(new CreateRoleRequest("editor", "Editor Again"), CancellationToken.None));
    }

    [Fact]
    public async Task SetPermissionsAsync_replaces_the_full_set()
    {
        var db = NewDb();
        var sut = new RoleService(db);
        var role = await sut.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);

        var permissionA = Permission.Create("articles.view", "View articles");
        var permissionB = Permission.Create("articles.edit", "Edit articles");
        var permissionC = Permission.Create("articles.delete", "Delete articles");
        db.Permissions.AddRange(permissionA, permissionB, permissionC);
        await db.SaveChangesAsync(CancellationToken.None);

        await sut.SetPermissionsAsync(role.Id, new AssignPermissionsRequest([permissionA.Id, permissionB.Id]), CancellationToken.None);
        var firstPass = await sut.GetPermissionIdsAsync(role.Id, CancellationToken.None);
        Assert.Equal(new[] { permissionA.Id, permissionB.Id }.ToHashSet(), firstPass.ToHashSet());

        // Replacing with {B, C} should drop A and add C, keep B.
        await sut.SetPermissionsAsync(role.Id, new AssignPermissionsRequest([permissionB.Id, permissionC.Id]), CancellationToken.None);
        var secondPass = await sut.GetPermissionIdsAsync(role.Id, CancellationToken.None);

        Assert.Equal(2, secondPass.Count);
        Assert.Contains(permissionB.Id, secondPass);
        Assert.Contains(permissionC.Id, secondPass);
        Assert.DoesNotContain(permissionA.Id, secondPass);
    }

    [Fact]
    public async Task SetPermissionsAsync_rejects_an_unknown_permission_id()
    {
        var db = NewDb();
        var sut = new RoleService(db);
        var role = await sut.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.SetPermissionsAsync(role.Id, new AssignPermissionsRequest([Guid.NewGuid()]), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_refuses_a_role_still_assigned_to_a_user()
    {
        var db = NewDb();
        var sut = new RoleService(db);
        var role = await sut.CreateAsync(new CreateRoleRequest("editor", "Editor"), CancellationToken.None);
        db.UserRoles.Add(UserRole.Create(Guid.NewGuid(), role.Id));
        await db.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => sut.DeleteAsync(role.Id, CancellationToken.None));
    }
}

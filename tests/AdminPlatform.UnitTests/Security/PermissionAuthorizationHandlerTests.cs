using System.Security.Claims;
using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace AdminPlatform.UnitTests.Security;

public class PermissionAuthorizationHandlerTests
{
    private static AuthorizationHandlerContext BuildContext(string requiredPermission, params string[] grantedPermissions)
    {
        var claims = grantedPermissions.Select(p => new Claim(AppClaimTypes.Permission, p));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(requiredPermission);

        return new AuthorizationHandlerContext([requirement], principal, resource: null);
    }

    [Fact]
    public async Task Succeeds_when_caller_has_the_exact_permission()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext("users.view", "users.view", "roles.view");

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_caller_lacks_the_permission()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext("users.delete", "users.view", "roles.view");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_caller_has_no_permission_claims_at_all()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext("users.view");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

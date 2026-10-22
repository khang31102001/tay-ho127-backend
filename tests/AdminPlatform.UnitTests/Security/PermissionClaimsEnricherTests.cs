using System.Security.Claims;
using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace AdminPlatform.UnitTests.Security;

public class PermissionClaimsEnricherTests
{
    private sealed class FakePermissionSource : IUserPermissionSource
    {
        private readonly UserPermissionsSnapshot _snapshot;

        public FakePermissionSource(params string[] permissions)
        {
            _snapshot = new UserPermissionsSnapshot(["Editor"], permissions);
        }

        public List<Guid> Requested { get; } = [];

        public Task<UserPermissionsSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            Requested.Add(userId);
            return Task.FromResult(_snapshot);
        }
    }

    private static ClaimsPrincipal Principal(string accountType, string? userId, params Claim[] extra)
    {
        var claims = new List<Claim> { new(AppClaimTypes.AccountType, accountType), new(AppClaimTypes.Role, "Editor") };
        if (userId is not null)
        {
            claims.Add(new Claim(AppClaimTypes.UserId, userId));
        }

        claims.AddRange(extra);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static string[] PermissionsOf(ClaimsPrincipal principal) =>
        principal.FindAll(AppClaimTypes.Permission).Select(c => c.Value).Order().ToArray();

    [Fact]
    public async Task Admin_principal_gets_its_permissions_from_the_server_side_source()
    {
        var userId = Guid.NewGuid();
        var principal = Principal(AccountTypes.Admin, userId.ToString());
        var source = new FakePermissionSource("users.view", "roles.view");

        await PermissionClaimsEnricher.EnrichAsync(principal, source, CancellationToken.None);

        Assert.Equal(["roles.view", "users.view"], PermissionsOf(principal));
        Assert.Equal([userId], source.Requested);
    }

    [Fact]
    public async Task Permission_claims_carried_by_the_token_are_dropped_not_trusted()
    {
        // A token issued before permissions left the JWT still carries them; the server-side source is the only authority.
        var principal = Principal(
            AccountTypes.Admin,
            Guid.NewGuid().ToString(),
            new Claim(AppClaimTypes.Permission, "stale.permission"),
            new Claim(AppClaimTypes.Permission, "users.delete"));
        var source = new FakePermissionSource("users.view");

        await PermissionClaimsEnricher.EnrichAsync(principal, source, CancellationToken.None);

        Assert.Equal(["users.view"], PermissionsOf(principal));
    }

    [Fact]
    public async Task User_with_no_permissions_ends_up_with_no_permission_claims()
    {
        var principal = Principal(AccountTypes.Admin, Guid.NewGuid().ToString(), new Claim(AppClaimTypes.Permission, "users.view"));

        await PermissionClaimsEnricher.EnrichAsync(principal, new FakePermissionSource(), CancellationToken.None);

        Assert.Empty(PermissionsOf(principal));
    }

    [Fact]
    public async Task Role_claims_from_the_token_are_left_alone()
    {
        var principal = Principal(AccountTypes.Admin, Guid.NewGuid().ToString());

        await PermissionClaimsEnricher.EnrichAsync(principal, new FakePermissionSource("users.view"), CancellationToken.None);

        Assert.Equal(["Editor"], principal.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToArray());
    }

    [Fact]
    public async Task Customer_principal_is_untouched_and_the_source_is_never_called()
    {
        var principal = Principal(AccountTypes.Customer, Guid.NewGuid().ToString());
        var source = new FakePermissionSource("users.view");

        await PermissionClaimsEnricher.EnrichAsync(principal, source, CancellationToken.None);

        Assert.Empty(PermissionsOf(principal));
        Assert.Empty(source.Requested);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Admin_principal_without_a_valid_user_id_is_untouched(string? userId)
    {
        var principal = Principal(AccountTypes.Admin, userId);
        var source = new FakePermissionSource("users.view");

        await PermissionClaimsEnricher.EnrichAsync(principal, source, CancellationToken.None);

        Assert.Empty(PermissionsOf(principal));
        Assert.Empty(source.Requested);
    }

    [Fact]
    public async Task Null_principal_is_ignored()
    {
        var source = new FakePermissionSource("users.view");

        await PermissionClaimsEnricher.EnrichAsync(null, source, CancellationToken.None);

        Assert.Empty(source.Requested);
    }

    [Fact]
    public async Task Enriched_principal_passes_and_fails_the_permission_requirement_correctly()
    {
        var principal = Principal(AccountTypes.Admin, Guid.NewGuid().ToString());
        await PermissionClaimsEnricher.EnrichAsync(principal, new FakePermissionSource("users.view"), CancellationToken.None);
        var handler = new PermissionAuthorizationHandler();

        var allowed = new AuthorizationHandlerContext([new PermissionRequirement("users.view")], principal, resource: null);
        var denied = new AuthorizationHandlerContext([new PermissionRequirement("users.delete")], principal, resource: null);
        await handler.HandleAsync(allowed);
        await handler.HandleAsync(denied);

        Assert.True(allowed.HasSucceeded);
        Assert.False(denied.HasSucceeded);
    }
}

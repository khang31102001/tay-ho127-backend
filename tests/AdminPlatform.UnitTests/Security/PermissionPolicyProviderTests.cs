using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Security;

public class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider CreateProvider() =>
        new(Options.Create(new AuthorizationOptions()));

    [Fact]
    public async Task Builds_a_dynamic_policy_for_any_Permission_prefixed_name()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor("users.view"));

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy!.Requirements.OfType<PermissionRequirement>());
        Assert.Equal("users.view", requirement.PermissionCode);
    }

    [Fact]
    public async Task Falls_back_to_the_default_provider_for_non_permission_policy_names()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("SomeOtherPolicy");

        Assert.Null(policy);
    }

    [Fact]
    public void NameFor_uses_the_documented_prefix()
    {
        Assert.Equal("Permission:users.view", PermissionPolicy.NameFor("users.view"));
    }
}

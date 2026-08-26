using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Common.Security;

/// <summary>Any policy named "Permission:{code}" is satisfied when the caller's `permission` claims
/// (embedded in the JWT at login/refresh) contain that code. Policies are resolved dynamically —
/// permission codes are data owned by the AccessControl module, not a fixed compile-time policy list.</summary>
public static class PermissionPolicy
{
    public const string Prefix = "Permission:";

    public static string NameFor(string permissionCode) => Prefix + permissionCode;
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Claims.Any(c => c.Type == AppClaimTypes.Permission && c.Value == requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Builds an <see cref="AuthorizationPolicy"/> on the fly for any "Permission:{code}" policy name,
/// instead of requiring every permission to be registered up front with AddAuthorization.</summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal))
        {
            var permissionCode = policyName[PermissionPolicy.Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}

/// <summary>Shorthand for <c>[Authorize(Policy = PermissionPolicy.NameFor(code))]</c>. See api-design.md §37.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
        : base(PermissionPolicy.NameFor(permissionCode))
    {
    }
}

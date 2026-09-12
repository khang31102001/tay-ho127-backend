using Microsoft.AspNetCore.Authorization;

namespace AdminPlatform.Common.Security;

/// <summary>Fixed (non-dynamic, unlike <see cref="PermissionPolicy"/>) authorization policies — one per
/// <see cref="AccountTypes"/> value. Registered once at the Host composition root (Program.cs) via
/// AddAuthorization(options => options.AddPolicy(...)).</summary>
public static class AccountTypePolicy
{
    public static string NameFor(string accountType) => "AccountType:" + accountType;
}

public sealed class AccountTypeRequirement : IAuthorizationRequirement
{
    public string AccountType { get; }

    public AccountTypeRequirement(string accountType)
    {
        AccountType = accountType;
    }
}

public sealed class AccountTypeAuthorizationHandler : AuthorizationHandler<AccountTypeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountTypeRequirement requirement)
    {
        if (context.User.Claims.Any(c => c.Type == AppClaimTypes.AccountType && c.Value == requirement.AccountType))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Shorthand for <c>[Authorize(Policy = AccountTypePolicy.NameFor(accountType))]</c> — use on any
/// endpoint that must reject a JWT from the wrong "realm" (Admin vs Customer) even when it doesn't also
/// require a specific permission (e.g. the caller's own profile/session endpoints).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireAccountTypeAttribute : AuthorizeAttribute
{
    public RequireAccountTypeAttribute(string accountType)
        : base(AccountTypePolicy.NameFor(accountType))
    {
    }
}

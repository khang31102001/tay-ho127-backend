using System.Security.Claims;
using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace AdminPlatform.UnitTests.Security;

public class AccountTypeAuthorizationHandlerTests
{
    private static AuthorizationHandlerContext BuildContext(string requiredAccountType, string? actualAccountType)
    {
        var claims = actualAccountType is null
            ? []
            : new[] { new Claim(AppClaimTypes.AccountType, actualAccountType) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var requirement = new AccountTypeRequirement(requiredAccountType);

        return new AuthorizationHandlerContext([requirement], principal, resource: null);
    }

    [Fact]
    public async Task Succeeds_when_the_account_type_claim_matches()
    {
        var handler = new AccountTypeAuthorizationHandler();
        var context = BuildContext(AccountTypes.Admin, AccountTypes.Admin);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_the_account_type_claim_is_the_other_realm()
    {
        var handler = new AccountTypeAuthorizationHandler();
        var context = BuildContext(AccountTypes.Admin, AccountTypes.Customer);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_there_is_no_account_type_claim_at_all()
    {
        var handler = new AccountTypeAuthorizationHandler();
        var context = BuildContext(AccountTypes.Admin, actualAccountType: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

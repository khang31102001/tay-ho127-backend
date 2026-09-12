namespace AdminPlatform.Common.Security;

/// <summary>A user's resolved role/permission codes. Shared shape between Identity's IUserPermissionsProvider
/// port (what it needs to issue a JWT) and AccessControl's IRolePermissionQueryService (what it exposes) —
/// living in Common lets both modules depend on it without referencing each other's project.</summary>
public sealed record UserPermissionsSnapshot(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)
{
    public static readonly UserPermissionsSnapshot Empty = new([], []);
}

/// <summary>Custom claim type names embedded in the access token by the token-issuing module (Identity for
/// admin users, Customer for customer accounts) and read back by <see cref="Abstractions.ICurrentUser"/> and
/// <see cref="AccountTypeAuthorizationHandler"/> everywhere else.</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
    public const string CurrentBrandId = "brand_id";
    public const string CurrentFiscalYearId = "fiscal_year_id";

    /// <summary>Distinguishes an Admin-issued JWT from a Customer-issued JWT when both are validated by the
    /// same JWT bearer scheme (same issuer/audience/signing key) — see <see cref="AccountTypes"/>.</summary>
    public const string AccountType = "account_type";
}

/// <summary>Values for <see cref="AppClaimTypes.AccountType"/>. Every access token this application issues
/// carries exactly one of these, so an Admin token and a Customer token can never be mistaken for each other
/// even though they share the same JWT bearer infrastructure (api-design.md §36-37).</summary>
public static class AccountTypes
{
    public const string Admin = "admin";
    public const string Customer = "customer";
}

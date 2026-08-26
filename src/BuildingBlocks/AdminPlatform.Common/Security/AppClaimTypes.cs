namespace AdminPlatform.Common.Security;

/// <summary>A user's resolved role/permission codes. Shared shape between Identity's IUserPermissionsProvider
/// port (what it needs to issue a JWT) and AccessControl's IRolePermissionQueryService (what it exposes) —
/// living in Common lets both modules depend on it without referencing each other's project.</summary>
public sealed record UserPermissionsSnapshot(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)
{
    public static readonly UserPermissionsSnapshot Empty = new([], []);
}

/// <summary>Custom claim type names embedded in the access token by the Identity module and read
/// back by <see cref="Abstractions.ICurrentUser"/> everywhere else.</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
    public const string CurrentBrandId = "brand_id";
    public const string CurrentFiscalYearId = "fiscal_year_id";
}

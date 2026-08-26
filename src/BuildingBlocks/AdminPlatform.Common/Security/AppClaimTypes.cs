namespace AdminPlatform.Common.Security;

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

namespace AdminPlatform.Common.Abstractions;

/// <summary>The authenticated caller, resolved from the current JWT. Never read HttpContext/ClaimsPrincipal
/// directly from Application/Infrastructure code — depend on this instead.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    Guid? CurrentBrandId { get; }
    Guid? CurrentFiscalYearId { get; }

    bool HasPermission(string permissionCode) => Permissions.Contains(permissionCode);
}

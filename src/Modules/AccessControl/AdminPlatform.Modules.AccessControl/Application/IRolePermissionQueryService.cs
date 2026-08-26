using AdminPlatform.Common.Security;

namespace AdminPlatform.Modules.AccessControl.Application;

/// <summary>AccessControl's public read contract, consumed by the Host composition root to implement
/// Identity's IUserPermissionsProvider port when a JWT is issued (login/refresh) — see architecture
/// assumption #6 and Program.cs's IdentityToAccessControlPermissionsAdapter.</summary>
public interface IRolePermissionQueryService
{
    Task<UserPermissionsSnapshot> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}

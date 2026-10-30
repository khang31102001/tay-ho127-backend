using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Common's IUserPermissionSource (the server-side source of an admin caller's permissions, no longer
/// carried inside the JWT) by delegating to AccessControl's exposed IRolePermissionQueryService. Lives in the Host for the
/// same reason as <see cref="IdentityPermissionsAdapter"/>. Registered behind CachedUserPermissionSource.</summary>
internal sealed class AccessControlPermissionSource : IUserPermissionSource
{
    private readonly IRolePermissionQueryService _rolePermissionQueryService;

    public AccessControlPermissionSource(IRolePermissionQueryService rolePermissionQueryService)
    {
        _rolePermissionQueryService = rolePermissionQueryService;
    }

    public Task<UserPermissionsSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        _rolePermissionQueryService.GetForUserAsync(userId, cancellationToken);
}

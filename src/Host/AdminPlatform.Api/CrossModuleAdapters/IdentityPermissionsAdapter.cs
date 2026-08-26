using AdminPlatform.Common.Security;
using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.Identity.Application;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Identity's IUserPermissionsProvider port by delegating to AccessControl's exposed
/// IRolePermissionQueryService. Lives here — not in either module — because the Host is the only project
/// allowed to reference both (architecture assumption #6: no module references another module's project).</summary>
internal sealed class IdentityPermissionsAdapter : IUserPermissionsProvider
{
    private readonly IRolePermissionQueryService _rolePermissionQueryService;

    public IdentityPermissionsAdapter(IRolePermissionQueryService rolePermissionQueryService)
    {
        _rolePermissionQueryService = rolePermissionQueryService;
    }

    public Task<UserPermissionsSnapshot> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        _rolePermissionQueryService.GetForUserAsync(userId, cancellationToken);
}

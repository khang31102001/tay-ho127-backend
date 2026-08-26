using AdminPlatform.Common.Security;

namespace AdminPlatform.Modules.Identity.Application;

/// <summary>Port the Identity module depends on to resolve a user's roles/permissions when issuing a JWT,
/// without referencing the AccessControl module's project directly. The real implementation lives in the
/// Host composition root, backed by AccessControl's exposed IRolePermissionQueryService — see
/// architecture assumption #6 and Program.cs.</summary>
public interface IUserPermissionsProvider
{
    Task<UserPermissionsSnapshot> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}

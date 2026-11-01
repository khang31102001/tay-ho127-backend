namespace AdminPlatform.Common.Security;

/// <summary>Resolves an admin user's roles and permissions on the server, so they do not have to travel inside the JWT.
/// Implemented by the Host composition root (it is the only project allowed to reach into AccessControl) and wrapped in
/// <see cref="CachedUserPermissionSource"/>.</summary>
public interface IUserPermissionSource
{
    Task<UserPermissionsSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken);
}

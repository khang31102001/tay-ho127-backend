using Microsoft.Extensions.Caching.Memory;

namespace AdminPlatform.Common.Security;

/// <summary>Short-lived in-memory cache in front of an <see cref="IUserPermissionSource"/>: permissions are looked up on
/// every authenticated admin request, but changing a user's roles only needs to show up within seconds, not instantly.
/// Failures are never cached — an exception from the inner source simply propagates.</summary>
public sealed class CachedUserPermissionSource : IUserPermissionSource
{
    /// <summary>How long a user's permissions are reused: a role/permission change takes effect within this time.</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(30);

    private readonly IUserPermissionSource _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _lifetime;

    public CachedUserPermissionSource(IUserPermissionSource inner, IMemoryCache cache, TimeSpan? lifetime = null)
    {
        _inner = inner;
        _cache = cache;
        _lifetime = lifetime ?? DefaultLifetime;
    }

    public async Task<UserPermissionsSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var key = $"user-permissions:{userId:N}";
        if (_cache.TryGetValue(key, out UserPermissionsSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await _inner.GetAsync(userId, cancellationToken);
        _cache.Set(key, snapshot, _lifetime);
        return snapshot;
    }
}

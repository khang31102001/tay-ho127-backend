using AdminPlatform.Common.Security;
using Microsoft.Extensions.Caching.Memory;

namespace AdminPlatform.UnitTests.Security;

public class CachedUserPermissionSourceTests
{
    private sealed class CountingSource : IUserPermissionSource
    {
        public int Calls { get; private set; }

        public bool FailNextCall { get; set; }

        public Task<UserPermissionsSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            Calls++;
            if (FailNextCall)
            {
                FailNextCall = false;
                throw new InvalidOperationException("database unavailable");
            }

            return Task.FromResult(new UserPermissionsSnapshot(["Editor"], [$"perm-{Calls}"]));
        }
    }

    private static CachedUserPermissionSource Create(CountingSource inner, TimeSpan? lifetime = null) =>
        new(inner, new MemoryCache(new MemoryCacheOptions()), lifetime);

    [Fact]
    public async Task Second_lookup_for_the_same_user_is_served_from_the_cache()
    {
        var inner = new CountingSource();
        var cached = Create(inner);
        var userId = Guid.NewGuid();

        var first = await cached.GetAsync(userId, CancellationToken.None);
        var second = await cached.GetAsync(userId, CancellationToken.None);

        Assert.Equal(1, inner.Calls);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Different_users_are_cached_separately()
    {
        var inner = new CountingSource();
        var cached = Create(inner);

        await cached.GetAsync(Guid.NewGuid(), CancellationToken.None);
        await cached.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Entry_expires_so_a_permission_change_is_picked_up()
    {
        var inner = new CountingSource();
        var cached = Create(inner, TimeSpan.FromMilliseconds(20));
        var userId = Guid.NewGuid();

        var before = await cached.GetAsync(userId, CancellationToken.None);
        await Task.Delay(150);
        var after = await cached.GetAsync(userId, CancellationToken.None);

        Assert.Equal(2, inner.Calls);
        Assert.NotEqual(before.Permissions, after.Permissions);
    }

    [Fact]
    public async Task A_failed_lookup_is_not_cached()
    {
        var inner = new CountingSource { FailNextCall = true };
        var cached = Create(inner);
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cached.GetAsync(userId, CancellationToken.None));
        var recovered = await cached.GetAsync(userId, CancellationToken.None);

        Assert.Equal(2, inner.Calls);
        Assert.Single(recovered.Permissions);
    }

    [Fact]
    public void Default_lifetime_is_short_enough_for_role_changes_to_show_up_quickly()
    {
        Assert.True(CachedUserPermissionSource.DefaultLifetime <= TimeSpan.FromMinutes(1));
    }
}

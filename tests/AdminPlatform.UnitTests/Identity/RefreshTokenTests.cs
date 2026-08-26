using AdminPlatform.Modules.Identity.Domain;

namespace AdminPlatform.UnitTests.Identity;

public class RefreshTokenTests
{
    [Fact]
    public void Issue_sets_expiry_from_lifetime()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", now, TimeSpan.FromDays(7), "device", "127.0.0.1");

        Assert.Equal(now.AddDays(7), token.ExpiresAtUtc);
        Assert.True(token.IsActive(now));
    }

    [Fact]
    public void IsActive_is_false_once_expired()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", now, TimeSpan.FromMinutes(1), null, null);

        var afterExpiry = now.AddMinutes(2);

        Assert.True(token.IsExpired(afterExpiry));
        Assert.False(token.IsActive(afterExpiry));
    }

    [Fact]
    public void Revoke_is_idempotent_and_keeps_first_replacement_link()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", now, TimeSpan.FromDays(7), null, null);
        var firstReplacement = Guid.NewGuid();

        token.Revoke(now, firstReplacement);
        token.Revoke(now.AddSeconds(1), Guid.NewGuid());

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(now));
        Assert.Equal(firstReplacement, token.ReplacedByTokenId);
    }
}

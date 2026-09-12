using System.Security.Claims;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application;
using AdminPlatform.Modules.Customer.Application.Auth;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Customer;

file sealed class PassthroughPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string hash, string providedPassword) => hash == $"hashed:{providedPassword}";
}

file sealed class FakeJwtTokenService : IJwtTokenService
{
    public AccessToken CreateAccessToken(IEnumerable<Claim> claims) => new("fake-access-token", DateTime.UtcNow.AddMinutes(15));

    public string GenerateRefreshToken() => Guid.NewGuid().ToString();

    public string HashRefreshToken(string rawToken) => "hash:" + rawToken;
}

file sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

file sealed class FakeGoogleIdTokenVerifier : IGoogleIdTokenVerifier
{
    private readonly GoogleIdentityPayload _payload;

    public FakeGoogleIdTokenVerifier(GoogleIdentityPayload payload) => _payload = payload;

    public Task<GoogleIdentityPayload> VerifyAsync(string idToken, CancellationToken cancellationToken) => Task.FromResult(_payload);
}

public class CustomerAuthServiceTests
{
    private static CustomerDbContext NewDb() =>
        new(new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CustomerAuthService NewSut(CustomerDbContext db, GoogleIdentityPayload? googlePayload = null, IDateTimeProvider? clock = null) => new(
        db,
        new PassthroughPasswordHasher(),
        new FakeJwtTokenService(),
        new FakeGoogleIdTokenVerifier(googlePayload ?? new GoogleIdentityPayload("sub", "google@example.com", true, "Google User")),
        clock ?? new FixedDateTimeProvider(),
        Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

    [Fact]
    public async Task RegisterAsync_creates_a_customer_and_local_identity()
    {
        var db = NewDb();
        var sut = NewSut(db);

        var tokens = await sut.RegisterAsync(new RegisterCustomerRequest("Jane Doe", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);
        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(1, await db.CustomerAuthIdentities.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_rejects_a_duplicate_email()
    {
        var db = NewDb();
        var sut = NewSut(db);
        await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "dup@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(new RegisterCustomerRequest("Other", "0909999999", "dup@example.com", "S3curePassw0rd"), null, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_rejects_a_duplicate_phone()
    {
        var db = NewDb();
        var sut = NewSut(db);
        await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(new RegisterCustomerRequest("Other", "0901234567", "other@example.com", "S3curePassw0rd"), null, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_succeeds_with_the_correct_password()
    {
        var db = NewDb();
        var sut = NewSut(db);
        await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        var tokens = await sut.LoginAsync(new CustomerLoginRequest("jane@example.com", "S3curePassw0rd", null), null, CancellationToken.None);

        Assert.NotEmpty(tokens.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_rejects_the_wrong_password()
    {
        var db = NewDb();
        var sut = NewSut(db);
        await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.LoginAsync(new CustomerLoginRequest("jane@example.com", "WrongPassword1", null), null, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_rejects_an_unknown_email()
    {
        var db = NewDb();
        var sut = NewSut(db);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.LoginAsync(new CustomerLoginRequest("nobody@example.com", "S3curePassw0rd", null), null, CancellationToken.None));
    }

    [Fact]
    public async Task GoogleLoginAsync_creates_a_new_customer_the_first_time()
    {
        var db = NewDb();
        var sut = NewSut(db, new GoogleIdentityPayload("google-sub-1", "newperson@example.com", true, "New Person"));

        var tokens = await sut.GoogleLoginAsync(new GoogleLoginRequest("id-token", null), null, CancellationToken.None);

        Assert.NotEmpty(tokens.AccessToken);
        Assert.Equal(1, await db.Customers.CountAsync());
        var identity = await db.CustomerAuthIdentities.SingleAsync();
        Assert.Equal(Modules.Customer.Domain.CustomerAuthProvider.Google, identity.Provider);
    }

    [Fact]
    public async Task GoogleLoginAsync_reuses_the_same_customer_on_a_second_login()
    {
        var db = NewDb();
        var sut = NewSut(db, new GoogleIdentityPayload("google-sub-1", "person@example.com", true, "Person"));

        var first = await sut.GoogleLoginAsync(new GoogleLoginRequest("id-token", null), null, CancellationToken.None);
        var second = await sut.GoogleLoginAsync(new GoogleLoginRequest("id-token", null), null, CancellationToken.None);

        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.NotEmpty(first.AccessToken);
        Assert.NotEmpty(second.AccessToken);
    }

    [Fact]
    public async Task GoogleLoginAsync_links_to_an_existing_local_account_with_the_same_verified_email()
    {
        var db = NewDb();
        var sut = NewSut(db, new GoogleIdentityPayload("google-sub-1", "jane@example.com", true, "Jane"));

        await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);
        await sut.GoogleLoginAsync(new GoogleLoginRequest("id-token", null), null, CancellationToken.None);

        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(2, await db.CustomerAuthIdentities.CountAsync());
    }

    [Fact]
    public async Task GoogleLoginAsync_rejects_an_unverified_email()
    {
        var db = NewDb();
        var sut = NewSut(db, new GoogleIdentityPayload("google-sub-1", "unverified@example.com", false, "Name"));

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GoogleLoginAsync(new GoogleLoginRequest("id-token", null), null, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_rotates_the_token_and_revokes_the_old_one()
    {
        var db = NewDb();
        var clock = new FixedDateTimeProvider();
        var sut = NewSut(db, clock: clock);
        var tokens = await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        var refreshed = await sut.RefreshAsync(tokens.RefreshToken, null, null, CancellationToken.None);

        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => sut.RefreshAsync(tokens.RefreshToken, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task LogoutAsync_revokes_the_refresh_token()
    {
        var db = NewDb();
        var sut = NewSut(db);
        var tokens = await sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None);

        await sut.LogoutAsync(tokens.RefreshToken, CancellationToken.None);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => sut.RefreshAsync(tokens.RefreshToken, null, null, CancellationToken.None));
    }
}

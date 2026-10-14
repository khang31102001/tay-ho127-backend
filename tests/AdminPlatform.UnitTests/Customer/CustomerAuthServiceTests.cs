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

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

file sealed class FakeGoogleIdTokenVerifier : IGoogleIdTokenVerifier
{
    private readonly GoogleIdentityPayload _payload;

    public FakeGoogleIdTokenVerifier(GoogleIdentityPayload payload) => _payload = payload;

    public Task<GoogleIdentityPayload> VerifyAsync(string idToken, CancellationToken cancellationToken) => Task.FromResult(_payload);
}

internal sealed class RecordingPhoneVerificationSender : IPhoneVerificationSender
{
    public List<(string Phone, string Code)> Sent { get; } = [];

    public bool Fail { get; set; }

    public string LastCode => Sent[^1].Code;

    public Task SendRegistrationCodeAsync(string phone, string code, TimeSpan validFor, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new InvalidOperationException("provider down");
        }

        Sent.Add((phone, code));
        return Task.CompletedTask;
    }
}

public class CustomerAuthServiceTests
{
    private static CustomerDbContext NewDb() =>
        new(new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CustomerAuthService NewSut(
        CustomerDbContext db,
        GoogleIdentityPayload? googlePayload = null,
        IDateTimeProvider? clock = null,
        IPhoneVerificationSender? sender = null,
        PhoneVerificationOptions? phoneVerification = null) => new(
        db,
        new PassthroughPasswordHasher(),
        new FakeJwtTokenService(),
        new FakeGoogleIdTokenVerifier(googlePayload ?? new GoogleIdentityPayload("sub", "google@example.com", true, "Google User")),
        clock ?? new FixedDateTimeProvider(),
        Options.Create(new JwtOptions { RefreshTokenDays = 7, SigningKey = "unit-test-signing-key" }),
        sender ?? new RecordingPhoneVerificationSender(),
        // The classic tests below register straight away; the OTP tests opt in with Required = true.
        Options.Create(phoneVerification ?? new PhoneVerificationOptions { Required = false }));

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

    // ---- Registration with a one-time code ----

    private static readonly RequestRegistrationOtpRequest JaneSignUp = new("Jane Doe", "0901234567", "jane@example.com", "S3curePassw0rd");

    private static (CustomerAuthService Sut, CustomerDbContext Db, RecordingPhoneVerificationSender Sender, FixedDateTimeProvider Clock) NewOtpSut(PhoneVerificationOptions? options = null)
    {
        var db = NewDb();
        var sender = new RecordingPhoneVerificationSender();
        var clock = new FixedDateTimeProvider();
        var sut = NewSut(db, clock: clock, sender: sender, phoneVerification: options ?? new PhoneVerificationOptions());
        return (sut, db, sender, clock);
    }

    [Fact]
    public async Task RegisterAsync_is_refused_while_phone_verification_is_required()
    {
        var (sut, db, _, _) = NewOtpSut();

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None));

        Assert.Empty(db.Customers);
    }

    [Fact]
    public async Task RequestRegistrationOtpAsync_sends_a_code_and_creates_no_customer_yet()
    {
        var (sut, db, sender, _) = NewOtpSut();

        await sut.RequestRegistrationOtpAsync(JaneSignUp with { Phone = "+84 901 234 567" }, CancellationToken.None);

        var sent = Assert.Single(sender.Sent);
        Assert.Equal("0901234567", sent.Phone);
        Assert.Matches(@"^\d{6}$", sent.Code);
        Assert.Empty(db.Customers);

        var pending = await db.PhoneVerifications.SingleAsync();
        Assert.DoesNotContain(sent.Code, pending.CodeHash);
        Assert.NotEqual("S3curePassw0rd", pending.PasswordHash);
    }

    [Fact]
    public async Task VerifyRegistrationOtpAsync_with_the_right_code_creates_a_verified_customer_and_signs_in()
    {
        var (sut, db, sender, _) = NewOtpSut();
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);

        var tokens = await sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", sender.LastCode, null), null, CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        var customer = await db.Customers.SingleAsync();
        Assert.Equal("0901234567", customer.Phone);
        Assert.NotNull(customer.PhoneVerifiedAtUtc);
        Assert.Empty(db.PhoneVerifications);

        // The password given at sign-up works for a normal login.
        var login = await sut.LoginAsync(new CustomerLoginRequest("jane@example.com", "S3curePassw0rd", null), null, CancellationToken.None);
        Assert.False(string.IsNullOrEmpty(login.AccessToken));
    }

    [Fact]
    public async Task VerifyRegistrationOtpAsync_rejects_a_wrong_code_and_locks_the_code_after_too_many_guesses()
    {
        var (sut, db, sender, _) = NewOtpSut();
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        var rightCode = sender.LastCode;
        var wrongCode = rightCode == "000000" ? "111111" : "000000";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
                sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", wrongCode, null), null, CancellationToken.None));
        }

        // Guessing is over: even the real code no longer works.
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", rightCode, null), null, CancellationToken.None));
        Assert.Empty(db.Customers);
    }

    [Fact]
    public async Task VerifyRegistrationOtpAsync_rejects_an_expired_code()
    {
        var (sut, db, sender, clock) = NewOtpSut();
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);

        clock.UtcNow = clock.UtcNow.AddMinutes(6);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", sender.LastCode, null), null, CancellationToken.None));
        Assert.Empty(db.Customers);
    }

    [Fact]
    public async Task VerifyRegistrationOtpAsync_rejects_a_phone_without_a_pending_code()
    {
        var (sut, _, _, _) = NewOtpSut();

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", "123456", null), null, CancellationToken.None));
    }

    [Fact]
    public async Task RequestRegistrationOtpAsync_enforces_the_resend_cooldown_and_a_new_code_replaces_the_old_one()
    {
        var (sut, _, sender, clock) = NewOtpSut();
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        var firstCode = sender.LastCode;

        await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None));

        clock.UtcNow = clock.UtcNow.AddSeconds(61);
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        var secondCode = sender.LastCode;

        if (firstCode != secondCode)
        {
            await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
                sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", firstCode, null), null, CancellationToken.None));
        }

        var tokens = await sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", secondCode, null), null, CancellationToken.None);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task RequestRegistrationOtpAsync_caps_the_codes_per_phone_per_window()
    {
        var (sut, _, sender, clock) = NewOtpSut(new PhoneVerificationOptions { MaxSendsPerWindow = 3 });

        for (var send = 0; send < 3; send++)
        {
            await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
            clock.UtcNow = clock.UtcNow.AddSeconds(61);
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None));
        Assert.Equal(3, sender.Sent.Count);

        clock.UtcNow = clock.UtcNow.AddMinutes(61);
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        Assert.Equal(4, sender.Sent.Count);
    }

    [Fact]
    public async Task RequestRegistrationOtpAsync_gives_the_attempt_back_when_the_provider_fails()
    {
        var (sut, _, sender, _) = NewOtpSut();
        sender.Fail = true;

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None));

        // No cooldown for a code that never left: the customer can try again straight away.
        sender.Fail = false;
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task RequestRegistrationOtpAsync_rejects_a_phone_or_email_that_is_already_registered()
    {
        var (sut, _, sender, _) = NewOtpSut();
        await sut.RequestRegistrationOtpAsync(JaneSignUp, CancellationToken.None);
        await sut.VerifyRegistrationOtpAsync(new VerifyRegistrationOtpRequest("0901234567", sender.LastCode, null), null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RequestRegistrationOtpAsync(JaneSignUp with { Phone = "0907654321" }, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RequestRegistrationOtpAsync(JaneSignUp with { Email = "other@example.com" }, CancellationToken.None));
    }

    [Theory]
    [InlineData("0901234567", true)]
    [InlineData("+84901234567", true)]
    [InlineData("84 901 234 567", true)]
    [InlineData("090-123-4567", true)]
    [InlineData("12345", false)]
    [InlineData("abcdefghij", false)]
    [InlineData("", false)]
    public void PhoneNumberNormalizer_accepts_vietnamese_mobile_formats(string phone, bool expectedValid)
    {
        Assert.Equal(expectedValid, PhoneNumberNormalizer.IsValid(phone));
        if (expectedValid)
        {
            Assert.Equal("0901234567", PhoneNumberNormalizer.Normalize(phone));
        }
    }
}

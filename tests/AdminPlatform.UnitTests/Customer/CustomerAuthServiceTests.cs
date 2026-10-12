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

file sealed class FakePhoneVerifier : ICustomerPhoneVerifier
{
    public List<(string Phone, string? Ip)> Sent { get; } = [];
    public List<(Guid ChallengeId, string Phone, string Code)> Verified { get; } = [];
    public PhoneVerificationOutcome Outcome { get; set; } = PhoneVerificationOutcome.Verified;
    public Exception? SendFailure { get; set; }
    public Guid IssuedChallengeId { get; } = Guid.NewGuid();

    public Task<PhoneVerificationIssued> SendRegistrationCodeAsync(string phone, string? requestIp, CancellationToken cancellationToken)
    {
        if (SendFailure is not null)
        {
            throw SendFailure;
        }

        Sent.Add((phone, requestIp));
        return Task.FromResult(new PhoneVerificationIssued(IssuedChallengeId, new DateTime(2026, 1, 1, 0, 5, 0, DateTimeKind.Utc), 60));
    }

    public Task<PhoneVerificationOutcome> VerifyRegistrationCodeAsync(Guid challengeId, string phone, string code, CancellationToken cancellationToken)
    {
        Verified.Add((challengeId, phone, code));
        return Task.FromResult(Outcome);
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
        ICustomerPhoneVerifier? phoneVerifier = null,
        bool phoneVerificationRequired = false) => new(
        db,
        new PassthroughPasswordHasher(),
        new FakeJwtTokenService(),
        new FakeGoogleIdTokenVerifier(googlePayload ?? new GoogleIdentityPayload("sub", "google@example.com", true, "Google User")),
        clock ?? new FixedDateTimeProvider(),
        Options.Create(new JwtOptions { RefreshTokenDays = 7 }),
        phoneVerifier ?? new FakePhoneVerifier(),
        Options.Create(new PhoneVerificationOptions { Required = phoneVerificationRequired }));

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

    // ---- Phone verification (one-time code) at registration ----

    private static readonly Guid Challenge = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static RegisterCustomerRequest WithOtp(string phone = "0901234567", string email = "jane@example.com", string code = "123456") =>
        new("Jane", phone, email, "S3curePassw0rd", Challenge, code);

    [Fact]
    public async Task With_verification_off_the_otp_fields_are_ignored_and_the_phone_is_not_marked_verified()
    {
        var db = NewDb();
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(db, phoneVerifier: verifier, phoneVerificationRequired: false);

        await sut.RegisterAsync(WithOtp(phone: "+84 901 234 567"), null, CancellationToken.None);

        Assert.Empty(verifier.Verified);
        var customer = await db.Customers.SingleAsync();
        Assert.Equal("0901234567", customer.Phone);
        Assert.Null(customer.PhoneVerifiedAtUtc);
    }

    [Fact]
    public async Task With_verification_on_a_missing_challenge_or_code_is_refused_before_anything_is_created()
    {
        var db = NewDb();
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(db, phoneVerifier: verifier, phoneVerificationRequired: true);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RegisterAsync(new RegisterCustomerRequest("Jane", "0901234567", "jane@example.com", "S3curePassw0rd"), null, CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RegisterAsync(WithOtp(code: "  "), null, CancellationToken.None));

        Assert.Empty(verifier.Verified);
        Assert.Equal(0, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task With_verification_on_a_right_code_creates_a_verified_customer_with_the_canonical_phone()
    {
        var db = NewDb();
        var clock = new FixedDateTimeProvider();
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(db, clock: clock, phoneVerifier: verifier, phoneVerificationRequired: true);

        var tokens = await sut.RegisterAsync(WithOtp(phone: "+84 901 234 567", code: " 654321 "), null, CancellationToken.None);

        Assert.NotEmpty(tokens.AccessToken);
        var verified = Assert.Single(verifier.Verified);
        Assert.Equal((Challenge, "0901234567", "654321"), verified);
        var customer = await db.Customers.SingleAsync();
        Assert.Equal("0901234567", customer.Phone);
        Assert.Equal(clock.UtcNow, customer.PhoneVerifiedAtUtc);
        var me = await sut.GetMeAsync(customer.Id, CancellationToken.None);
        Assert.True(me.PhoneVerified);
    }

    [Theory]
    [InlineData(PhoneVerificationOutcome.Invalid, "không đúng")]
    [InlineData(PhoneVerificationOutcome.Expired, "hết hạn")]
    [InlineData(PhoneVerificationOutcome.TooManyAttempts, "quá số lần")]
    public async Task With_verification_on_a_refused_code_creates_nothing_and_explains_why(PhoneVerificationOutcome outcome, string expectedWords)
    {
        var db = NewDb();
        var sut = NewSut(db, phoneVerifier: new FakePhoneVerifier { Outcome = outcome }, phoneVerificationRequired: true);

        var exception = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.RegisterAsync(WithOtp(), null, CancellationToken.None));

        Assert.Contains(expectedWords, exception.Message);
        Assert.Equal(0, await db.Customers.CountAsync());
        Assert.Equal(0, await db.CustomerAuthIdentities.CountAsync());
    }

    [Fact]
    public async Task With_verification_on_a_taken_email_or_phone_is_reported_without_spending_the_code()
    {
        var db = NewDb();
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(db, phoneVerifier: verifier, phoneVerificationRequired: true);
        await sut.RegisterAsync(WithOtp(), null, CancellationToken.None);
        verifier.Verified.Clear();

        await Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(WithOtp(phone: "0909999999"), null, CancellationToken.None));
        // The same number written another way is still the same number.
        await Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(WithOtp(phone: "+84901234567", email: "other@example.com"), null, CancellationToken.None));

        Assert.Empty(verifier.Verified);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdefghij")]
    [InlineData("+1 202 555 0100")]
    public async Task With_verification_on_a_phone_that_is_not_vietnamese_is_refused_before_a_code_is_checked(string phone)
    {
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(NewDb(), phoneVerifier: verifier, phoneVerificationRequired: true);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.RegisterAsync(WithOtp(phone: phone), null, CancellationToken.None));

        Assert.Empty(verifier.Verified);
    }

    [Fact]
    public async Task RequestRegistrationOtp_is_refused_while_verification_is_off()
    {
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(NewDb(), phoneVerifier: verifier, phoneVerificationRequired: false);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("0901234567", "jane@example.com"), null, CancellationToken.None));

        Assert.Empty(verifier.Sent);
    }

    [Fact]
    public async Task RequestRegistrationOtp_sends_one_code_to_the_canonical_phone_and_returns_what_the_client_needs()
    {
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(NewDb(), phoneVerifier: verifier, phoneVerificationRequired: true);

        var response = await sut.RequestRegistrationOtpAsync(
            new RequestRegistrationOtpRequest("+84 901 234 567", " Jane@Example.com "), "10.1.2.3", CancellationToken.None);

        var sent = Assert.Single(verifier.Sent);
        Assert.Equal(("0901234567", "10.1.2.3"), sent);
        Assert.Equal(verifier.IssuedChallengeId, response.ChallengeId);
        Assert.Equal(60, response.ResendAfterSeconds);
    }

    [Fact]
    public async Task RequestRegistrationOtp_does_not_send_a_paid_code_to_a_phone_or_email_that_is_already_registered()
    {
        var db = NewDb();
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(db, phoneVerifier: verifier, phoneVerificationRequired: true);
        await sut.RegisterAsync(WithOtp(), null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("0909999999", "jane@example.com"), null, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("+84901234567", "new@example.com"), null, CancellationToken.None));

        Assert.Empty(verifier.Sent);
    }

    [Fact]
    public async Task RequestRegistrationOtp_rejects_a_phone_that_is_not_vietnamese_and_passes_sending_limits_through()
    {
        var verifier = new FakePhoneVerifier();
        var sut = NewSut(NewDb(), phoneVerifier: verifier, phoneVerificationRequired: true);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("12345", "a@example.com"), null, CancellationToken.None));

        verifier.SendFailure = new TooManyRequestsException("slow down", 42);
        var limited = await Assert.ThrowsAsync<TooManyRequestsException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("0901234567", "a@example.com"), null, CancellationToken.None));
        Assert.Equal(42, limited.RetryAfterSeconds);

        verifier.SendFailure = new ServiceUnavailableException("provider down");
        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            sut.RequestRegistrationOtpAsync(new RequestRegistrationOtpRequest("0901234567", "a@example.com"), null, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "a@example.com", false)]
    [InlineData("12345", "a@example.com", false)]
    [InlineData("0901234567", "", false)]
    [InlineData("0901234567", "not-an-email", false)]
    [InlineData("0901234567", "a@example.com", true)]
    [InlineData("+84 901 234 567", "a@example.com", true)]
    public void The_otp_request_validator_checks_the_phone_and_the_email(string phone, string email, bool expectedValid)
    {
        var result = new RequestRegistrationOtpRequestValidator().Validate(new RequestRegistrationOtpRequest(phone, email));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void The_register_request_still_binds_when_a_client_sends_no_otp_fields()
    {
        // ASP.NET Core binds JSON with web defaults: old clients (and the existing tests) must keep working.
        var web = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);

        var old = System.Text.Json.JsonSerializer.Deserialize<RegisterCustomerRequest>(
            """{"fullName":"Jane","phone":"0901234567","email":"jane@example.com","password":"S3curePassw0rd"}""", web)!;
        var current = System.Text.Json.JsonSerializer.Deserialize<RegisterCustomerRequest>(
            """{"fullName":"Jane","phone":"0901234567","email":"jane@example.com","password":"S3curePassw0rd","otpChallengeId":"11111111-1111-1111-1111-111111111111","otpCode":"123456"}""", web)!;

        Assert.Null(old.OtpChallengeId);
        Assert.Null(old.OtpCode);
        Assert.Equal(Challenge, current.OtpChallengeId);
        Assert.Equal("123456", current.OtpCode);
    }
}

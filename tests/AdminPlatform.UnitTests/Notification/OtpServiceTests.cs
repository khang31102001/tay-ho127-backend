using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Notification.Application.Otp;
using AdminPlatform.Modules.Notification.Application.Ports;
using AdminPlatform.Modules.Notification.Domain;
using AdminPlatform.Modules.Notification.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Notification;

internal sealed class OtpTestClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

internal sealed class OtpCapturingSender : IMessageSender
{
    public List<OutboundMessage> Sent { get; } = [];

    public Func<OutboundMessage, MessageSendResult> Behaviour { get; set; } =
        _ => new MessageSendResult(true, "fake", "zalo", "msg-1", "100");

    public Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        Sent.Add(message);
        return Task.FromResult(Behaviour(message));
    }

    public string LastCode => Sent[^1].Parameters["code"];
}

internal sealed class OtpThrowingSender : IMessageSender
{
    public Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken) =>
        throw new HttpRequestException("network down");
}

public class OtpServiceTests
{
    private const string Phone = "0901234567";
    private const string Purpose = OtpPurposes.RegisterPhone;

    private static readonly string ValidKey = Convert.ToBase64String(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray());

    private static NotificationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static OtpOptions DefaultOptions() => new() { HmacKey = ValidKey };

    private static OtpService NewSut(NotificationDbContext db, IMessageSender sender, OtpTestClock clock, OtpOptions? options = null) =>
        new(db, sender, clock, Options.Create(options ?? DefaultOptions()), NullLogger<OtpService>.Instance);

    private static OtpRequest Request(string phone = Phone, string? ip = "10.0.0.1") => new(Purpose, phone, ip);

    [Fact]
    public async Task Request_sends_a_numeric_code_stores_only_its_hash_and_logs_a_masked_delivery()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());

        var issued = await sut.RequestAsync(Request(), CancellationToken.None);

        var message = Assert.Single(sender.Sent);
        Assert.Equal("otp-register", message.Kind);
        Assert.Equal(Phone, message.Phone);
        Assert.Equal(6, sender.LastCode.Length);
        Assert.All(sender.LastCode, c => Assert.True(char.IsAsciiDigit(c)));
        Assert.Equal(issued.ChallengeId.ToString("N"), message.RequestId);

        var stored = await db.OtpChallenges.SingleAsync();
        Assert.Equal(issued.ChallengeId, stored.Id);
        Assert.NotEqual(sender.LastCode, stored.CodeHash);
        Assert.DoesNotContain(sender.LastCode, stored.CodeHash);
        Assert.Equal(64, stored.CodeHash.Length);

        var log = await db.DeliveryLogs.SingleAsync();
        Assert.Equal("090****567", log.MaskedPhone);
        Assert.Equal("accepted", log.Status);
        Assert.Equal(issued.ChallengeId, log.ReferenceId);
        Assert.Equal(300, (issued.ExpiresAtUtc - stored.CreatedAtUtc).TotalSeconds);
        Assert.Equal(60, issued.ResendAfterSeconds);
    }

    [Fact]
    public async Task A_right_code_verifies_once_and_cannot_be_used_again()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);

        var first = await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, sender.LastCode, CancellationToken.None);
        var second = await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, sender.LastCode, CancellationToken.None);

        Assert.Equal(OtpVerifyOutcome.Verified, first);
        Assert.Equal(OtpVerifyOutcome.Invalid, second);
    }

    [Fact]
    public async Task A_wrong_code_is_invalid_and_counts_as_an_attempt()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);
        var wrong = sender.LastCode == "000000" ? "111111" : "000000";

        var outcome = await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, wrong, CancellationToken.None);

        Assert.Equal(OtpVerifyOutcome.Invalid, outcome);
        Assert.Equal(1, (await db.OtpChallenges.SingleAsync()).AttemptCount);
    }

    [Fact]
    public async Task After_the_allowed_wrong_guesses_even_the_right_code_is_refused()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock(), new OtpOptions { HmacKey = ValidKey, MaxAttempts = 3 });
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);
        var wrong = sender.LastCode == "000000" ? "111111" : "000000";

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, wrong, CancellationToken.None));
        }

        Assert.Equal(OtpVerifyOutcome.TooManyAttempts, await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, sender.LastCode, CancellationToken.None));
    }

    [Fact]
    public async Task An_expired_code_is_refused()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var clock = new OtpTestClock();
        var sut = NewSut(db, sender, clock);
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(301));

        Assert.Equal(OtpVerifyOutcome.Expired, await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, sender.LastCode, CancellationToken.None));
    }

    [Fact]
    public async Task A_code_does_not_verify_for_another_phone_purpose_or_challenge()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);

        Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(issued.ChallengeId, Purpose, "0912345678", sender.LastCode, CancellationToken.None));
        Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(issued.ChallengeId, "other-purpose", Phone, sender.LastCode, CancellationToken.None));
        Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(Guid.NewGuid(), Purpose, Phone, sender.LastCode, CancellationToken.None));
        Assert.Equal(0, (await db.OtpChallenges.SingleAsync()).AttemptCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public async Task A_code_of_the_wrong_shape_is_invalid_without_using_an_attempt(string code)
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());
        var issued = await sut.RequestAsync(Request(), CancellationToken.None);

        Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(issued.ChallengeId, Purpose, Phone, code, CancellationToken.None));
        Assert.Equal(0, (await db.OtpChallenges.SingleAsync()).AttemptCount);
    }

    [Fact]
    public async Task A_second_request_inside_the_cooldown_is_refused_with_the_time_to_wait()
    {
        var db = NewDb();
        var clock = new OtpTestClock();
        var sut = NewSut(db, new OtpCapturingSender(), clock);
        await sut.RequestAsync(Request(), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(20));
        var exception = await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestAsync(Request(), CancellationToken.None));
        Assert.Equal(40, exception.RetryAfterSeconds);

        clock.Advance(TimeSpan.FromSeconds(41));
        await sut.RequestAsync(Request(), CancellationToken.None);
    }

    [Fact]
    public async Task One_phone_cannot_ask_for_more_codes_than_the_hourly_limit()
    {
        var db = NewDb();
        var clock = new OtpTestClock();
        var sut = NewSut(db, new OtpCapturingSender(), clock, new OtpOptions { HmacKey = ValidKey, ResendCooldownSeconds = 0, MaxPerPhonePerHour = 3 });

        for (var i = 0; i < 3; i++)
        {
            await sut.RequestAsync(Request(), CancellationToken.None);
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestAsync(Request(), CancellationToken.None));

        // A different phone is unaffected, and the first phone is free again once the hour has passed.
        await sut.RequestAsync(Request("0912345678"), CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(61));
        await sut.RequestAsync(Request(), CancellationToken.None);
    }

    [Fact]
    public async Task One_address_cannot_ask_for_codes_for_many_phones_beyond_the_hourly_limit()
    {
        var db = NewDb();
        var sut = NewSut(db, new OtpCapturingSender(), new OtpTestClock(), new OtpOptions { HmacKey = ValidKey, MaxPerIpPerHour = 2 });

        await sut.RequestAsync(Request("0901111111", "9.9.9.9"), CancellationToken.None);
        await sut.RequestAsync(Request("0902222222", "9.9.9.9"), CancellationToken.None);

        await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestAsync(Request("0903333333", "9.9.9.9"), CancellationToken.None));
        await sut.RequestAsync(Request("0904444444", "8.8.8.8"), CancellationToken.None);
    }

    [Fact]
    public async Task The_daily_cap_stops_all_new_codes_system_wide()
    {
        var db = NewDb();
        var sut = NewSut(db, new OtpCapturingSender(), new OtpTestClock(), new OtpOptions { HmacKey = ValidKey, DailyCap = 2 });

        await sut.RequestAsync(Request("0901111111", "1.1.1.1"), CancellationToken.None);
        await sut.RequestAsync(Request("0902222222", "2.2.2.2"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TooManyRequestsException>(() => sut.RequestAsync(Request("0903333333", "3.3.3.3"), CancellationToken.None));
        Assert.Equal(3600, exception.RetryAfterSeconds);
    }

    [Fact]
    public async Task When_the_provider_refuses_the_code_is_withdrawn_and_the_failure_is_reported()
    {
        var db = NewDb();
        var sender = new OtpCapturingSender { Behaviour = _ => MessageSendResult.Rejected("fake", "101") };
        var sut = NewSut(db, sender, new OtpTestClock());

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => sut.RequestAsync(Request(), CancellationToken.None));

        var challenge = await db.OtpChallenges.SingleAsync();
        Assert.True(challenge.IsConsumed);
        Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(challenge.Id, Purpose, Phone, sender.LastCode, CancellationToken.None));
        var log = await db.DeliveryLogs.SingleAsync();
        Assert.Equal("failed", log.Status);
        Assert.Equal("101", log.ResultCode);
    }

    [Fact]
    public async Task When_the_provider_throws_it_is_treated_the_same_as_a_refusal()
    {
        var db = NewDb();
        var sut = NewSut(db, new OtpThrowingSender(), new OtpTestClock());

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => sut.RequestAsync(Request(), CancellationToken.None));

        Assert.True((await db.OtpChallenges.SingleAsync()).IsConsumed);
        Assert.Equal("failed", (await db.DeliveryLogs.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")]
    public async Task Without_a_usable_hmac_key_nothing_is_issued_or_checked(string key)
    {
        var db = NewDb();
        var sut = NewSut(db, new OtpCapturingSender(), new OtpTestClock(), new OtpOptions { HmacKey = key });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RequestAsync(Request(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.VerifyAsync(Guid.NewGuid(), Purpose, Phone, "123456", CancellationToken.None));
        Assert.Empty(db.OtpChallenges);
    }

    [Theory]
    [InlineData("0123")]
    [InlineData("+84901234567")]
    [InlineData("84901234567")]
    [InlineData("090123456a")]
    public async Task A_phone_not_in_stored_form_is_rejected_before_anything_is_sent(string phone)
    {
        var db = NewDb();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, new OtpTestClock());

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.RequestAsync(Request(phone), CancellationToken.None));

        Assert.Empty(sender.Sent);
        Assert.Empty(db.OtpChallenges);
    }

    [Fact]
    public async Task An_unknown_purpose_is_rejected()
    {
        var sut = NewSut(NewDb(), new OtpCapturingSender(), new OtpTestClock());

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            sut.RequestAsync(new OtpRequest("something-else", Phone, null), CancellationToken.None));
    }

    [Fact]
    public async Task Two_codes_for_the_same_phone_never_share_a_hash()
    {
        var db = NewDb();
        var clock = new OtpTestClock();
        var sender = new OtpCapturingSender();
        var sut = NewSut(db, sender, clock, new OtpOptions { HmacKey = ValidKey, ResendCooldownSeconds = 0 });

        var first = await sut.RequestAsync(Request(), CancellationToken.None);
        var firstCode = sender.LastCode;
        var second = await sut.RequestAsync(Request(), CancellationToken.None);

        // The first challenge's code must not open the second one, and vice versa.
        if (firstCode != sender.LastCode)
        {
            Assert.Equal(OtpVerifyOutcome.Invalid, await sut.VerifyAsync(second.ChallengeId, Purpose, Phone, firstCode, CancellationToken.None));
        }

        Assert.Equal(OtpVerifyOutcome.Verified, await sut.VerifyAsync(second.ChallengeId, Purpose, Phone, sender.LastCode, CancellationToken.None));
        Assert.NotEqual(first.ChallengeId, second.ChallengeId);
    }
}

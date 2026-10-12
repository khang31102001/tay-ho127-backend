using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Notification.Application.Ports;
using AdminPlatform.Modules.Notification.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Notification.Application.Otp;

/// <summary>Issues one-time codes and checks them. The code is random, sent once, and stored only as a keyed hash
/// bound to the challenge, purpose and phone. Everything that can be abused for money (sending codes) or access
/// (guessing them) is limited here, in one place.</summary>
public sealed class OtpService : IOtpService
{
    private const int MaxConcurrencyRetries = 5;

    private readonly INotificationDbContext _db;
    private readonly IMessageSender _sender;
    private readonly IDateTimeProvider _clock;
    private readonly IOptions<OtpOptions> _options;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        INotificationDbContext db,
        IMessageSender sender,
        IDateTimeProvider clock,
        IOptions<OtpOptions> options,
        ILogger<OtpService> logger)
    {
        _db = db;
        _sender = sender;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public async Task<OtpIssued> RequestAsync(OtpRequest request, CancellationToken cancellationToken)
    {
        var options = ValidatedOptions();
        var key = ReadKey(options);

        var messageKind = OtpPurposes.MessageKindFor(request.Purpose)
            ?? throw new BusinessRuleValidationException("Mục đích xác minh không hợp lệ.");
        if (!IsStoredPhoneForm(request.Phone))
        {
            throw new BusinessRuleValidationException("Số điện thoại không hợp lệ.");
        }

        var now = _clock.UtcNow;
        await EnforceLimitsAsync(request, options, now, cancellationToken);

        var challengeId = Guid.NewGuid();
        var code = GenerateCode(options.CodeLength);
        var challenge = OtpChallenge.Issue(
            challengeId,
            request.Purpose,
            request.Phone,
            ComputeHash(key, challengeId, request.Purpose, request.Phone, code),
            now,
            TimeSpan.FromSeconds(options.LifetimeSeconds),
            request.RequestIp);
        _db.OtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        var message = new OutboundMessage(
            messageKind,
            request.Phone,
            new Dictionary<string, string> { ["code"] = code },
            challengeId.ToString("N"),
            challengeId);
        var result = await TrySendAsync(message, challengeId, cancellationToken);

        _db.DeliveryLogs.Add(DeliveryLog.Record(
            result.Provider,
            result.Channel ?? "unknown",
            messageKind,
            challengeId,
            result.Accepted ? "accepted" : "failed",
            result.ProviderMessageId,
            result.ResultCode,
            message.RequestId,
            PhoneMasking.Mask(request.Phone),
            now));

        if (!result.Accepted)
        {
            // A code the user never received must not stay usable.
            challenge.Consume(now);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ServiceUnavailableException("Không thể gửi mã xác minh lúc này. Vui lòng thử lại sau ít phút.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new OtpIssued(challengeId, challenge.ExpiresAtUtc, options.ResendCooldownSeconds);
    }

    public async Task<OtpVerifyOutcome> VerifyAsync(
        Guid challengeId, string purpose, string phone, string code, CancellationToken cancellationToken)
    {
        var options = ValidatedOptions();
        var key = ReadKey(options);

        if (!IsDigits(code, options.CodeLength))
        {
            return OtpVerifyOutcome.Invalid;
        }

        var challenge = await _db.OtpChallenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.Purpose == purpose && c.Phone == phone, cancellationToken);
        if (challenge is null)
        {
            return OtpVerifyOutcome.Invalid;
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var now = _clock.UtcNow;
            if (challenge.IsConsumed)
            {
                return OtpVerifyOutcome.Invalid;
            }

            if (challenge.IsExpired(now))
            {
                return OtpVerifyOutcome.Expired;
            }

            if (challenge.IsOutOfAttempts(options.MaxAttempts))
            {
                return OtpVerifyOutcome.TooManyAttempts;
            }

            challenge.RegisterAttempt();
            var matches = HashesMatch(challenge.CodeHash, ComputeHash(key, challenge.Id, challenge.Purpose, challenge.Phone, code));
            if (matches)
            {
                challenge.Consume(now);
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return matches ? OtpVerifyOutcome.Verified : OtpVerifyOutcome.Invalid;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request changed this challenge at the same time. Start again from what it now says, so
                // parallel guesses are each counted and a code cannot be used twice.
                await _db.Entry(challenge).ReloadAsync(cancellationToken);
            }
        }

        return OtpVerifyOutcome.Invalid;
    }

    private async Task EnforceLimitsAsync(OtpRequest request, OtpOptions options, DateTime now, CancellationToken cancellationToken)
    {
        var cooldown = TimeSpan.FromSeconds(options.ResendCooldownSeconds);
        var lastRequestedAt = await _db.OtpChallenges.AsNoTracking()
            .Where(c => c.Phone == request.Phone && c.Purpose == request.Purpose)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => (DateTime?)c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (lastRequestedAt is { } last && now - last < cooldown)
        {
            var wait = SecondsUntil(last + cooldown, now);
            throw new TooManyRequestsException($"Bạn vừa yêu cầu mã xác minh. Vui lòng chờ {wait} giây để nhận mã mới.", wait);
        }

        await EnforceHourlyLimitAsync(
            _db.OtpChallenges.AsNoTracking().Where(c => c.Phone == request.Phone),
            options.MaxPerPhonePerHour,
            now,
            "Số điện thoại này đã yêu cầu quá nhiều mã xác minh. Vui lòng thử lại sau.",
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.RequestIp))
        {
            var ip = request.RequestIp.Trim();
            await EnforceHourlyLimitAsync(
                _db.OtpChallenges.AsNoTracking().Where(c => c.RequestIp == ip),
                options.MaxPerIpPerHour,
                now,
                "Có quá nhiều yêu cầu mã xác minh từ thiết bị của bạn. Vui lòng thử lại sau.",
                cancellationToken);
        }

        var sentToday = await _db.OtpChallenges.AsNoTracking().CountAsync(c => c.CreatedAtUtc > now.AddHours(-24), cancellationToken);
        if (sentToday >= options.DailyCap)
        {
            _logger.LogWarning("The daily OTP cap of {DailyCap} was reached; new codes are refused until older ones age out.", options.DailyCap);
            throw new TooManyRequestsException("Hệ thống đang xử lý quá nhiều yêu cầu. Vui lòng thử lại sau.", 3600);
        }
    }

    private static async Task EnforceHourlyLimitAsync(
        IQueryable<OtpChallenge> scope, int max, DateTime now, string message, CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(-1);
        var times = await scope
            .Where(c => c.CreatedAtUtc > windowStart)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => c.CreatedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
        if (times.Count >= max)
        {
            throw new TooManyRequestsException(message, SecondsUntil(times[0].AddHours(1), now));
        }
    }

    private async Task<MessageSendResult> TrySendAsync(OutboundMessage message, Guid challengeId, CancellationToken cancellationToken)
    {
        try
        {
            return await _sender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Never log the message: it holds the code. The challenge id is enough to find the delivery log row.
            _logger.LogError(exception, "Sending the one-time code for challenge {ChallengeId} failed.", challengeId);
            return MessageSendResult.Rejected("unknown", "exception");
        }
    }

    private OtpOptions ValidatedOptions()
    {
        var options = _options.Value;
        if (options.CodeLength is < 4 or > 9
            || options.LifetimeSeconds < 30
            || options.MaxAttempts < 1
            || options.ResendCooldownSeconds < 0
            || options.MaxPerPhonePerHour < 1
            || options.MaxPerIpPerHour < 1
            || options.DailyCap < 1)
        {
            throw new InvalidOperationException($"The '{OtpOptions.SectionName}' settings are out of range.");
        }

        return options;
    }

    private static byte[] ReadKey(OtpOptions options)
    {
        try
        {
            var key = Convert.FromBase64String(options.HmacKey ?? string.Empty);
            if (key.Length >= OtpOptions.MinHmacKeyBytes)
            {
                return key;
            }
        }
        catch (FormatException)
        {
            // handled below, with the same message as a key that is too short
        }

        throw new InvalidOperationException(
            $"{OtpOptions.SectionName}:HmacKey must be set to a base64 value of at least {OtpOptions.MinHmacKeyBytes} bytes.");
    }

    private static string GenerateCode(int length)
    {
        var upperBound = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(upperBound).ToString("D" + length, CultureInfo.InvariantCulture);
    }

    private static string ComputeHash(byte[] key, Guid challengeId, string purpose, string phone, string code)
    {
        var data = Encoding.UTF8.GetBytes($"{challengeId:N}|{purpose}|{phone}|{code}");
        return Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
    }

    private static bool HashesMatch(string storedHash, string candidateHash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(storedHash), Encoding.ASCII.GetBytes(candidateHash));

    private static bool IsDigits(string? value, int length) =>
        value is not null && value.Length == length && value.All(char.IsAsciiDigit);

    private static bool IsStoredPhoneForm(string? phone) => phone is not null && phone.Length == 10 && phone[0] == '0' && phone.All(char.IsAsciiDigit);

    private static int SecondsUntil(DateTime target, DateTime now) => (int)Math.Ceiling((target - now).TotalSeconds);
}

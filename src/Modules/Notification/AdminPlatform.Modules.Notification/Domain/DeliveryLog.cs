using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Notification.Domain;

/// <summary>Append-only record that a message was handed to a provider, and what the provider answered. It never
/// holds the message text, the one-time code or the full phone number — only a masked number — so the log can be
/// read by staff without exposing a customer's data.</summary>
public sealed class DeliveryLog : Entity
{
    public const int MaxProviderLength = 64;
    public const int MaxChannelLength = 16;
    public const int MaxKindLength = 64;
    public const int MaxStatusLength = 16;
    public const int MaxProviderMessageIdLength = 128;
    public const int MaxResultCodeLength = 32;
    public const int MaxRequestIdLength = 64;
    public const int MaxMaskedPhoneLength = 32;

    public string Provider { get; private set; } = string.Empty;

    /// <summary>"zalo" or "sms" — the channel that carried the message.</summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>What was sent, e.g. "otp-register" or "order-placed".</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>The OtpChallenge or order the message was about, when there is one.</summary>
    public Guid? ReferenceId { get; private set; }

    public string Status { get; private set; } = string.Empty;
    public string? ProviderMessageId { get; private set; }
    public string? ResultCode { get; private set; }
    public string? RequestId { get; private set; }
    public string MaskedPhone { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private DeliveryLog()
    {
        // EF Core
    }

    public static DeliveryLog Record(
        string provider,
        string channel,
        string kind,
        Guid? referenceId,
        string status,
        string? providerMessageId,
        string? resultCode,
        string? requestId,
        string maskedPhone,
        DateTime nowUtc)
    {
        return new DeliveryLog
        {
            Id = Guid.NewGuid(),
            Provider = Guard.NotNullOrWhiteSpace(provider, nameof(provider)).Trim(),
            Channel = Guard.NotNullOrWhiteSpace(channel, nameof(channel)).Trim(),
            Kind = Guard.NotNullOrWhiteSpace(kind, nameof(kind)).Trim(),
            ReferenceId = referenceId,
            Status = Guard.NotNullOrWhiteSpace(status, nameof(status)).Trim(),
            ProviderMessageId = providerMessageId,
            ResultCode = resultCode,
            RequestId = requestId,
            MaskedPhone = Guard.NotNullOrWhiteSpace(maskedPhone, nameof(maskedPhone)).Trim(),
            CreatedAtUtc = nowUtc,
        };
    }
}

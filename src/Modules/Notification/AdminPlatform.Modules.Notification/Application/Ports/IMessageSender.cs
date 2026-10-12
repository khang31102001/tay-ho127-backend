namespace AdminPlatform.Modules.Notification.Application.Ports;

/// <summary>Hands one templated message to a provider (Zalo, SMS, ...). The implementation owns everything
/// provider-specific — endpoint, authentication, which template id a <see cref="OutboundMessage.Kind"/> maps to.</summary>
public interface IMessageSender
{
    /// <summary>Returns a result for every outcome the provider can give, including "refused"; it throws only for
    /// the unexpected (network down, bad configuration). The caller treats both as "not sent".</summary>
    Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken);
}

/// <param name="Kind">What is being sent, e.g. "otp-register" or "order-placed" — the sender picks the template from it.</param>
/// <param name="Phone">Recipient in its stored form ("0901234567").</param>
/// <param name="Parameters">Template values by name (e.g. "code"). Sensitive: never logged by the caller.</param>
/// <param name="RequestId">Caller-chosen id (at most 50 characters) the provider uses to drop a repeated request.</param>
/// <param name="ReferenceId">The challenge or order the message is about, for the delivery log.</param>
public sealed record OutboundMessage(
    string Kind,
    string Phone,
    IReadOnlyDictionary<string, string> Parameters,
    string RequestId,
    Guid? ReferenceId);

/// <summary>"Accepted" only means the provider took the request, not that the phone received it.</summary>
public sealed record MessageSendResult(
    bool Accepted,
    string Provider,
    string? Channel,
    string? ProviderMessageId,
    string? ResultCode)
{
    public static MessageSendResult Rejected(string provider, string? resultCode) => new(false, provider, null, null, resultCode);
}

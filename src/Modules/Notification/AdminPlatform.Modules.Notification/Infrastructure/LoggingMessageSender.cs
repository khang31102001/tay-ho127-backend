using AdminPlatform.Modules.Notification.Application;
using AdminPlatform.Modules.Notification.Application.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Notification.Infrastructure;

public sealed class LoggingMessageSenderOptions
{
    public const string SectionName = "Notification:LoggingSender";

    /// <summary>Also logs the template values (for an OTP, the code itself) so a developer can finish a flow without a
    /// real provider. Honoured ONLY in the Development environment, whatever its value elsewhere.</summary>
    public bool LogParametersInDevelopment { get; set; }
}

/// <summary>The default sender: delivers nothing, only logs that a message would have gone out. A real provider's
/// sender, registered first, replaces it. Logs a masked phone number, never the full one.</summary>
internal sealed class LoggingMessageSender : IMessageSender
{
    private readonly ILogger<LoggingMessageSender> _logger;
    private readonly bool _isDevelopment;
    private readonly bool _logParameters;

    public LoggingMessageSender(
        ILogger<LoggingMessageSender> logger,
        IHostEnvironment environment,
        IOptions<LoggingMessageSenderOptions> options)
    {
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();
        _logParameters = _isDevelopment && options.Value.LogParametersInDevelopment;
    }

    public Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        if (_logParameters)
        {
            _logger.LogWarning(
                "[DEV ONLY] No message provider is configured; '{Kind}' to {Phone} would carry {Parameters}.",
                message.Kind,
                PhoneMasking.Mask(message.Phone),
                string.Join(", ", message.Parameters.Select(pair => $"{pair.Key}={pair.Value}")));
        }
        else
        {
            _logger.LogWarning(
                "No message provider is configured; '{Kind}' to {Phone} was not delivered.", message.Kind, PhoneMasking.Mask(message.Phone));
        }

        // Only in Development is "logged" good enough to count as sent (so a developer can finish a flow locally).
        // Anywhere else it is a refusal: a user must never be told a code was sent when nothing could deliver it.
        return Task.FromResult(_isDevelopment
            ? new MessageSendResult(true, "logging", "log", null, "logged")
            : MessageSendResult.Rejected("logging", "no-provider"));
    }
}

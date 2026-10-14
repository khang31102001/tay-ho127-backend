using AdminPlatform.Modules.Customer.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdminPlatform.Modules.Customer.Infrastructure;

/// <summary>Used while eSMS is not configured. In Development it writes the code to the log so the flow can be
/// tried without a provider; anywhere else it refuses to "send", because a code that only goes to a log is no
/// verification at all and printing it would leak it.</summary>
internal sealed class LoggingPhoneVerificationSender : IPhoneVerificationSender
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LoggingPhoneVerificationSender> _logger;

    public LoggingPhoneVerificationSender(IHostEnvironment environment, ILogger<LoggingPhoneVerificationSender> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task SendRegistrationCodeAsync(string phone, string code, TimeSpan validFor, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException("No phone verification provider is configured (set Esms:ApiKey and Esms:SecretKey).");
        }

        _logger.LogWarning("DEV ONLY - registration code for {Phone}: {Code} (valid {Minutes} min). Configure Esms:* to send it for real.",
            phone, code, validFor.TotalMinutes);
        return Task.CompletedTask;
    }
}

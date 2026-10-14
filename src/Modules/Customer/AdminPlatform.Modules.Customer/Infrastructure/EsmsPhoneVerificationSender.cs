using AdminPlatform.Common.Messaging;
using AdminPlatform.Modules.Customer.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Customer.Infrastructure;

/// <summary>Sends the registration code through eSMS.vn: a Zalo ZNS message and/or an SMS, per Esms:Channel.</summary>
internal sealed class EsmsPhoneVerificationSender : IPhoneVerificationSender
{
    private readonly IEsmsClient _esms;
    private readonly EsmsOptions _options;
    private readonly ILogger<EsmsPhoneVerificationSender> _logger;

    public EsmsPhoneVerificationSender(IEsmsClient esms, IOptions<EsmsOptions> options, ILogger<EsmsPhoneVerificationSender> logger)
    {
        _esms = esms;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendRegistrationCodeAsync(string phone, string code, TimeSpan validFor, CancellationToken cancellationToken)
    {
        var channel = _options.Channel.Trim();

        if (channel.Equals("Sms", StringComparison.OrdinalIgnoreCase))
        {
            await SendSmsAsync(phone, code, cancellationToken);
            return;
        }

        try
        {
            await _esms.SendZaloAsync(phone, _options.ZaloOtpTemplateId, [code], cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            && channel.Equals("ZaloThenSms", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(exception, "Zalo delivery failed for {Phone}; falling back to SMS.", phone);
            await SendSmsAsync(phone, code, cancellationToken);
        }
    }

    private Task SendSmsAsync(string phone, string code, CancellationToken cancellationToken) =>
        _esms.SendSmsAsync(phone, _options.SmsOtpContent.Replace("{code}", code, StringComparison.Ordinal), cancellationToken);
}

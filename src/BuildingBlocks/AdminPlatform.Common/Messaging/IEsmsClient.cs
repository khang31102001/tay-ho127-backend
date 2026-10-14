namespace AdminPlatform.Common.Messaging;

/// <summary>Thin client for the eSMS.vn API, shared by every module that messages customers (registration
/// codes, order notices). It only delivers; what to say and to whom is the caller's business. Each method
/// throws when eSMS does not accept the message.</summary>
public interface IEsmsClient
{
    /// <summary>Zalo ZNS: <paramref name="templateParams"/> fill the approved template's parameters in order.</summary>
    Task SendZaloAsync(string phone, string templateId, IReadOnlyList<string> templateParams, CancellationToken cancellationToken);

    Task SendSmsAsync(string phone, string content, CancellationToken cancellationToken);
}

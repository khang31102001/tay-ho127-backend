namespace AdminPlatform.Modules.Customer.Application;

/// <summary>Delivers a one-time code to a phone (Zalo / SMS). Customer registration depends on this port only;
/// the provider (eSMS today) is an Infrastructure detail and can be swapped without touching the flow.</summary>
public interface IPhoneVerificationSender
{
    /// <summary>Throws when the code could not be delivered.</summary>
    Task SendRegistrationCodeAsync(string phone, string code, TimeSpan validFor, CancellationToken cancellationToken);
}

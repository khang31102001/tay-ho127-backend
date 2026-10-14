namespace AdminPlatform.Modules.Customer.Application;

/// <summary>Bound from the "PhoneVerification" configuration section: how registration proves a phone number
/// with a one-time code.</summary>
public sealed class PhoneVerificationOptions
{
    public const string SectionName = "PhoneVerification";

    /// <summary>When true (the default) an account can only be created by confirming a code sent to the phone;
    /// the plain <c>POST /customer/auth/register</c> is refused. Switch off only for local work or tests.</summary>
    public bool Required { get; set; } = true;

    public int CodeLength { get; set; } = 6;
    public int LifetimeMinutes { get; set; } = 5;

    /// <summary>Wrong guesses allowed per code; after that the code is dead and a new one must be requested.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Codes one phone number may ask for within <see cref="SendWindowMinutes"/> — every code costs money.</summary>
    public int MaxSendsPerWindow { get; set; } = 5;

    public int SendWindowMinutes { get; set; } = 60;

    /// <summary>Secret mixed into the code hash. Empty = reuse the JWT signing key, so no extra setting is needed.</summary>
    public string HashKey { get; set; } = string.Empty;
}

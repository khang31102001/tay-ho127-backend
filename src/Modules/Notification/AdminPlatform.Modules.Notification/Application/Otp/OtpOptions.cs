namespace AdminPlatform.Modules.Notification.Application.Otp;

/// <summary>Section "Notification:Otp". The limits protect customers (no code floods) and the budget (every code
/// sent costs money, so a script requesting codes in a loop is a direct financial attack).</summary>
public sealed class OtpOptions
{
    public const string SectionName = "Notification:Otp";
    public const int MinHmacKeyBytes = 32;

    /// <summary>Base64 of at least 32 random bytes — keys the hash of every code. Set it in the server environment;
    /// without it no code can be issued or checked.</summary>
    public string HmacKey { get; set; } = string.Empty;

    public int CodeLength { get; set; } = 6;
    public int LifetimeSeconds { get; set; } = 300;
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Minimum wait between two codes for the same phone and purpose.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    public int MaxPerPhonePerHour { get; set; } = 5;
    public int MaxPerIpPerHour { get; set; } = 20;

    /// <summary>Codes the whole system may send in any rolling 24 hours — the last line of defence against SMS pumping.</summary>
    public int DailyCap { get; set; } = 500;
}

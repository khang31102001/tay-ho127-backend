namespace AdminPlatform.Modules.Customer.Application;

/// <summary>Bound from the "GoogleAuth" configuration section. ClientId is the OAuth 2.0 Web/Android/iOS
/// client id Google issued for this application — required so ID-token verification can check the
/// token's `aud` claim actually targets us (never trust a token without an audience match).</summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; set; } = string.Empty;
}

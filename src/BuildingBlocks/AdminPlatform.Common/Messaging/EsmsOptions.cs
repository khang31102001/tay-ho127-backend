namespace AdminPlatform.Common.Messaging;

/// <summary>Bound from the "Esms" configuration section (eSMS.vn: Zalo ZNS + SMS). ApiKey and SecretKey are
/// secrets: they come from environment variables / user-secrets, never from a committed file. With no keys
/// nothing is sent — each feature falls back to its logging behaviour.</summary>
public sealed class EsmsOptions
{
    public const string SectionName = "Esms";

    public string BaseUrl { get; set; } = "https://rest.esms.vn/MainService.svc/json/";
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>eSMS sandbox: the request is validated, nothing reaches the phone.</summary>
    public bool Sandbox { get; set; }

    // --- Zalo ZNS ---
    public string ZaloOaId { get; set; } = string.Empty;

    // --- SMS ---
    /// <summary>eSMS SmsType (8 = fixed-number OTP route, 2 = brandname customer care).</summary>
    public string SmsType { get; set; } = "8";

    public string SmsBrandName { get; set; } = string.Empty;

    // --- Registration one-time code (Customer) ---
    /// <summary>"Zalo" (ZNS only), "Sms" (SMS only) or "ZaloThenSms" (SMS when Zalo cannot deliver, e.g. the
    /// number has no Zalo).</summary>
    public string Channel { get; set; } = "Zalo";

    /// <summary>The approved ZNS template that carries the code as its only parameter.</summary>
    public string ZaloOtpTemplateId { get; set; } = string.Empty;

    /// <summary>Must match the template registered with eSMS; <c>{code}</c> is replaced by the code.</summary>
    public string SmsOtpContent { get; set; } = "{code} la ma xac minh dang ky tai khoan cua ban";

    // --- "Your order was placed" (Sales) ---
    /// <summary>The approved ZNS template for a placed order. Empty = no Zalo order message is sent.</summary>
    public string ZaloOrderPlacedTemplateId { get; set; } = string.Empty;

    /// <summary>What fills the template's parameters, in the template's order. Known names: CustomerName,
    /// OrderCode, Phone, TotalAmount (whole VND, no separators).</summary>
    public string[] ZaloOrderPlacedParams { get; set; } = ["CustomerName", "OrderCode", "TotalAmount"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SecretKey);
}

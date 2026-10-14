using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Common.Messaging;

/// <summary>eSMS.vn over HTTP. eSMS answers HTTP 200 with a CodeResult in the body; "100" means accepted.</summary>
internal sealed class EsmsClient : IEsmsClient
{
    private const string AcceptedCode = "100";
    private const string ZaloEndpoint = "SendZaloMessage_V4_post_json/";
    private const string SmsEndpoint = "SendMultipleMessage_V4_post_json/";

    private readonly HttpClient _httpClient;
    private readonly EsmsOptions _options;
    private readonly ILogger<EsmsClient> _logger;

    public EsmsClient(HttpClient httpClient, IOptions<EsmsOptions> options, ILogger<EsmsClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendZaloAsync(string phone, string templateId, IReadOnlyList<string> templateParams, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ZaloOaId) || string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("Esms:ZaloOaId and a Zalo template id are required to send a Zalo message.");
        }

        return PostAsync(ZaloEndpoint, new Dictionary<string, object?>
        {
            ["ApiKey"] = _options.ApiKey,
            ["SecretKey"] = _options.SecretKey,
            ["Phone"] = NormalizePhone(phone),
            ["Params"] = templateParams,
            ["TempID"] = templateId,
            ["OAID"] = _options.ZaloOaId,
            ["Sandbox"] = _options.Sandbox ? "1" : "0",
            ["RequestId"] = Guid.NewGuid().ToString("N"),
        }, "Zalo", phone, cancellationToken);
    }

    public Task SendSmsAsync(string phone, string content, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["ApiKey"] = _options.ApiKey,
            ["SecretKey"] = _options.SecretKey,
            ["Phone"] = NormalizePhone(phone),
            ["Content"] = content,
            ["SmsType"] = _options.SmsType,
            ["IsUnicode"] = "0",
            ["Sandbox"] = _options.Sandbox ? "1" : "0",
            ["RequestId"] = Guid.NewGuid().ToString("N"),
        };
        if (!string.IsNullOrWhiteSpace(_options.SmsBrandName))
        {
            body["Brandname"] = _options.SmsBrandName;
        }

        return PostAsync(SmsEndpoint, body, "SMS", phone, cancellationToken);
    }

    /// <summary>eSMS wants the local form 09xxxxxxxx; "+84 901 234 567" and "84901234567" become that.</summary>
    internal static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (digits.StartsWith("+84", StringComparison.Ordinal))
        {
            return "0" + digits[3..];
        }

        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length is 11 or 12)
        {
            return "0" + digits[2..];
        }

        return digits.TrimStart('+');
    }

    private async Task PostAsync(string endpoint, Dictionary<string, object?> body, string channel, string phone, CancellationToken cancellationToken)
    {
        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"eSMS {channel} request failed with HTTP {(int)response.StatusCode}.");
        }

        string? resultCode = null;
        string? errorMessage = null;
        try
        {
            using var json = JsonDocument.Parse(responseText);
            resultCode = json.RootElement.TryGetProperty("CodeResult", out var codeResult) ? codeResult.ToString() : null;
            errorMessage = json.RootElement.TryGetProperty("ErrorMessage", out var message) ? message.GetString() : null;
        }
        catch (JsonException)
        {
            // Handled below: an unreadable answer is a failure.
        }

        if (resultCode != AcceptedCode)
        {
            // The message content (a code, an order) is never logged — only the provider's verdict.
            _logger.LogWarning("eSMS {Channel} to {Phone} was not accepted: CodeResult={CodeResult} {ErrorMessage}", channel, phone, resultCode, errorMessage);
            throw new InvalidOperationException($"eSMS {channel} was not accepted (CodeResult={resultCode ?? "unknown"}).");
        }
    }
}

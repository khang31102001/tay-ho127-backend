using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdminPlatform.Modules.Notification.Application;
using AdminPlatform.Modules.Notification.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Notification.Infrastructure;

/// <summary>Section "Notification:Esms". The endpoint is the provider's documented multi-channel API.</summary>
public sealed class EsmsOptions
{
    public const string SectionName = "Notification:Esms";

    public string Endpoint { get; set; } = "https://rest.esms.vn/MainService.svc/json/MultiChannelMessage/";
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>Sends a templated message through eSMS's multi-channel API: Zalo (ZNS) first, and eSMS itself falls back to
/// SMS when Zalo cannot deliver. Settings come from the "esms" connection (Admin → Tích hợp); the API key and secret come
/// from server configuration. When the connection is missing or switched off, the fallback sender handles the message.
///
/// ASSUMPTIONS made without the final eSMS details — each is a setting or one line here, not a design change:
///  * the OTP Zalo template has exactly ONE variable, the code;
///  * the SMS fallback text is the setting 'otpRegisterSmsContent' with '{code}' replaced by the code, sent as a
///    customer-care SMS (SmsType "2", overridable by the 'smsType' setting) under the setting 'brandname';
///  * the phone goes out as stored ("0901234567");
///  * eSMS may report "accepted" (CodeResult 100) before the phone has actually received anything.
/// </summary>
public sealed class EsmsMessageSender : IMessageSender
{
    public const string ProviderCode = "esms";

    private const string DefaultSmsType = "2";

    /// <summary>What each kind of message needs: which settings hold its Zalo template id and SMS text, and the order of
    /// the Zalo template variables (a variable's position is its meaning to eSMS).</summary>
    private sealed record Template(string TemplateIdSetting, string SmsContentSetting, string[] ParameterNames);

    private static readonly Dictionary<string, Template> Templates = new(StringComparer.Ordinal)
    {
        ["otp-register"] = new("otpRegisterTemplateId", "otpRegisterSmsContent", ["code"]),
    };

    private static readonly string[] RequiredSecrets = ["apiKey", "secretKey"];

    private readonly HttpClient _http;
    private readonly IIntegrationConnectionProvider _connections;
    private readonly IMessageSender _fallback;
    private readonly EsmsOptions _options;
    private readonly ILogger<EsmsMessageSender> _logger;

    public EsmsMessageSender(
        HttpClient http,
        IIntegrationConnectionProvider connections,
        LoggingMessageSender fallback,
        IOptions<EsmsOptions> options,
        ILogger<EsmsMessageSender> logger)
    {
        _http = http;
        _connections = connections;
        _fallback = fallback;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        var connection = await _connections.FindAsync(ProviderCode, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            return await _fallback.SendAsync(message, cancellationToken);
        }

        if (!Templates.TryGetValue(message.Kind, out var template))
        {
            _logger.LogError("eSMS has no template for message kind '{Kind}'.", message.Kind);
            return MessageSendResult.Rejected(ProviderCode, "unsupported-kind");
        }

        var request = BuildRequest(message, connection, template, out var configurationProblem);
        if (request is null)
        {
            _logger.LogError("The eSMS connection is not fully configured for '{Kind}': {Problem}.", message.Kind, configurationProblem);
            return MessageSendResult.Rejected(ProviderCode, "not-configured");
        }

        return await PostAsync(request, message, cancellationToken);
    }

    private JsonObject? BuildRequest(OutboundMessage message, ProviderConnection connection, Template template, out string problem)
    {
        var missing = new List<string>();
        foreach (var secret in RequiredSecrets.Where(name => !connection.Secrets.ContainsKey(name)))
        {
            missing.Add($"secret {secret}");
        }

        string Setting(string key)
        {
            if (connection.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            missing.Add($"setting {key}");
            return string.Empty;
        }

        var oaId = Setting("oaId");
        var brandname = Setting("brandname");
        var templateId = Setting(template.TemplateIdSetting);
        var smsContent = Setting(template.SmsContentSetting);
        var smsType = connection.Settings.TryGetValue("smsType", out var configuredType) && !string.IsNullOrWhiteSpace(configuredType)
            ? configuredType
            : DefaultSmsType;

        var parameterValues = new List<string>();
        foreach (var name in template.ParameterNames)
        {
            if (message.Parameters.TryGetValue(name, out var value))
            {
                parameterValues.Add(value);
                smsContent = smsContent.Replace("{" + name + "}", value, StringComparison.Ordinal);
            }
            else
            {
                missing.Add($"parameter {name}");
            }
        }

        if (missing.Count > 0)
        {
            problem = string.Join(", ", missing);
            return null;
        }

        problem = string.Empty;
        var sandbox = connection.IsSandbox ? "1" : "0";
        return new JsonObject
        {
            ["ApiKey"] = connection.Secrets["apiKey"],
            ["SecretKey"] = connection.Secrets["secretKey"],
            ["Phone"] = message.Phone,
            // eSMS requires exactly this order and both channels: Zalo first, SMS as the fallback.
            ["Channels"] = new JsonArray("zalo", "sms"),
            ["Data"] = new JsonArray
            {
                new JsonObject
                {
                    ["TempID"] = templateId,
                    ["Params"] = new JsonArray(parameterValues.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray()),
                    ["OAID"] = oaId,
                    ["RequestId"] = message.RequestId,
                    ["Sandbox"] = sandbox,
                    ["SendingMode"] = "1",
                },
                new JsonObject
                {
                    ["Content"] = smsContent,
                    ["IsUnicode"] = "0",
                    ["SmsType"] = smsType,
                    ["Brandname"] = brandname,
                    ["RequestId"] = message.RequestId,
                    ["Sandbox"] = sandbox,
                },
            },
        };
    }

    private async Task<MessageSendResult> PostAsync(JsonObject request, OutboundMessage message, CancellationToken cancellationToken)
    {
        var endpoint = new Uri(_options.Endpoint, UriKind.Absolute);
        if (endpoint.Scheme != Uri.UriSchemeHttps)
        {
            // The body carries the API key and secret: never send it over plain HTTP.
            throw new InvalidOperationException($"'{EsmsOptions.SectionName}:Endpoint' must be an https address.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(endpoint, request, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("eSMS did not answer within {Seconds}s for request {RequestId}.", _options.TimeoutSeconds, message.RequestId);
            return MessageSendResult.Rejected(ProviderCode, "timeout");
        }
        catch (HttpRequestException exception)
        {
            // The exception text can name the host but never the body; the body is never logged.
            _logger.LogWarning(exception, "eSMS could not be reached for request {RequestId}.", message.RequestId);
            return MessageSendResult.Rejected(ProviderCode, "unreachable");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("eSMS answered HTTP {Status} for request {RequestId}.", (int)response.StatusCode, message.RequestId);
                return MessageSendResult.Rejected(ProviderCode, $"http-{(int)response.StatusCode}");
            }

            return await ReadResultAsync(response, message, timeout.Token);
        }
    }

    private async Task<MessageSendResult> ReadResultAsync(HttpResponseMessage response, OutboundMessage message, CancellationToken cancellationToken)
    {
        string? code;
        string? providerMessageId;
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            code = ReadText(document.RootElement, "CodeResult");
            providerMessageId = ReadText(document.RootElement, "SMSID");
        }
        catch (JsonException)
        {
            _logger.LogWarning("eSMS sent an unreadable answer for request {RequestId}.", message.RequestId);
            return MessageSendResult.Rejected(ProviderCode, "bad-response");
        }

        if (code == "100")
        {
            return new MessageSendResult(true, ProviderCode, "zalo+sms", providerMessageId, code);
        }

        // 101 and 789 are configuration mistakes an operator must fix; say so plainly (no secrets, no body).
        _logger.LogError(
            "eSMS refused request {RequestId} with CodeResult {CodeResult}{Hint}.",
            message.RequestId,
            code ?? "(none)",
            code switch
            {
                "101" => " - the API key or secret is wrong",
                "789" => " - the Zalo template is not set up for this OA id",
                _ => string.Empty,
            });
        return MessageSendResult.Rejected(ProviderCode, code ?? "no-code");
    }

    private static string? ReadText(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
}

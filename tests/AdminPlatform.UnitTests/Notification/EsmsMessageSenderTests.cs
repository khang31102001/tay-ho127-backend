using System.Net;
using System.Text;
using System.Text.Json;
using AdminPlatform.Modules.Notification.Application.Ports;
using AdminPlatform.Modules.Notification.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Notification;

internal sealed class EsmsStubHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public string? LastBody { get; private set; }
    public Uri? LastUri { get; private set; }
    public Func<HttpResponseMessage> Respond { get; set; } = () => Json("""{"CodeResult":"100","CountRegenerate":0,"SMSID":"sms-42"}""");

    public static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        LastUri = request.RequestUri;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return Respond();
    }
}

internal sealed class EsmsTimeoutHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new TaskCanceledException("simulated provider timeout");
}

internal sealed class EsmsFakeConnections : IIntegrationConnectionProvider
{
    public ProviderConnection? Connection { get; set; }

    public Task<ProviderConnection?> FindAsync(string providerCode, CancellationToken cancellationToken) => Task.FromResult(Connection);
}

internal sealed class EsmsFakeEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "tests";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class EsmsListLogger<T> : ILogger<T>
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Lines.Add(formatter(state, exception) + (exception is null ? string.Empty : " | " + exception));
}

public class EsmsMessageSenderTests
{
    private const string Phone = "0901234567";
    private const string ApiKey = "api-key-AAAA";
    private const string SecretKey = "secret-key-BBBB";
    private const string Code = "482913";

    private static OutboundMessage Otp(string kind = "otp-register") => new(
        kind, Phone, new Dictionary<string, string> { ["code"] = Code }, "req-0123456789abcdef0123456789ab", Guid.NewGuid());

    private static ProviderConnection Connection(
        bool enabled = true,
        bool sandbox = true,
        Dictionary<string, string>? settings = null,
        Dictionary<string, string>? secrets = null) =>
        new(
            "esms",
            enabled,
            sandbox,
            settings ?? new Dictionary<string, string>
            {
                ["oaId"] = "1234567890",
                ["brandname"] = "TAYHO127",
                ["otpRegisterTemplateId"] = "540610",
                ["otpRegisterSmsContent"] = "{code} la ma xac minh dang ky Tay Ho 127 cua ban",
            },
            secrets ?? new Dictionary<string, string> { ["apiKey"] = ApiKey, ["secretKey"] = SecretKey });

    private sealed record Rig(
        EsmsMessageSender Sut,
        EsmsStubHandler Handler,
        EsmsFakeConnections Connections,
        EsmsListLogger<EsmsMessageSender> Log);

    private static Rig NewRig(
        ProviderConnection? connection,
        HttpMessageHandler? handler = null,
        string environment = "Production",
        EsmsOptions? options = null)
    {
        var stub = handler as EsmsStubHandler ?? new EsmsStubHandler();
        var connections = new EsmsFakeConnections { Connection = connection };
        var log = new EsmsListLogger<EsmsMessageSender>();
        var fallback = new LoggingMessageSender(
            NullLogger<LoggingMessageSender>.Instance,
            new EsmsFakeEnvironment { EnvironmentName = environment },
            Options.Create(new LoggingMessageSenderOptions()));
        var sut = new EsmsMessageSender(
            new HttpClient(handler ?? stub),
            connections,
            fallback,
            Options.Create(options ?? new EsmsOptions()),
            log);
        return new Rig(sut, stub, connections, log);
    }

    [Fact]
    public async Task It_posts_the_documented_multi_channel_request_to_the_endpoint()
    {
        var rig = NewRig(Connection(sandbox: true));

        var result = await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.Equal("https://rest.esms.vn/MainService.svc/json/MultiChannelMessage/", rig.Handler.LastUri!.ToString());
        using var body = JsonDocument.Parse(rig.Handler.LastBody!);
        var root = body.RootElement;
        Assert.Equal(ApiKey, root.GetProperty("ApiKey").GetString());
        Assert.Equal(SecretKey, root.GetProperty("SecretKey").GetString());
        Assert.Equal(Phone, root.GetProperty("Phone").GetString());
        Assert.Equal(["zalo", "sms"], root.GetProperty("Channels").EnumerateArray().Select(c => c.GetString()!).ToArray());

        var zalo = root.GetProperty("Data")[0];
        Assert.Equal("540610", zalo.GetProperty("TempID").GetString());
        Assert.Equal("1234567890", zalo.GetProperty("OAID").GetString());
        Assert.Equal([Code], zalo.GetProperty("Params").EnumerateArray().Select(p => p.GetString()!).ToArray());
        Assert.Equal("req-0123456789abcdef0123456789ab", zalo.GetProperty("RequestId").GetString());
        Assert.Equal("1", zalo.GetProperty("Sandbox").GetString());

        var sms = root.GetProperty("Data")[1];
        Assert.Equal($"{Code} la ma xac minh dang ky Tay Ho 127 cua ban", sms.GetProperty("Content").GetString());
        Assert.Equal("TAYHO127", sms.GetProperty("Brandname").GetString());
        Assert.Equal("2", sms.GetProperty("SmsType").GetString());
        Assert.Equal("1", sms.GetProperty("Sandbox").GetString());

        Assert.True(result.Accepted);
        Assert.Equal("esms", result.Provider);
        Assert.Equal("sms-42", result.ProviderMessageId);
        Assert.Equal("100", result.ResultCode);
    }

    [Fact]
    public async Task A_live_connection_turns_sandbox_off_and_the_sms_type_can_be_overridden()
    {
        var settings = new Dictionary<string, string>
        {
            ["oaId"] = "1", ["brandname"] = "B", ["otpRegisterTemplateId"] = "9", ["otpRegisterSmsContent"] = "{code}", ["smsType"] = "8",
        };
        var rig = NewRig(Connection(sandbox: false, settings: settings));

        await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        using var body = JsonDocument.Parse(rig.Handler.LastBody!);
        Assert.Equal("0", body.RootElement.GetProperty("Data")[0].GetProperty("Sandbox").GetString());
        Assert.Equal("8", body.RootElement.GetProperty("Data")[1].GetProperty("SmsType").GetString());
    }

    [Theory]
    [InlineData("101")]
    [InlineData("789")]
    [InlineData("99")]
    public async Task A_provider_refusal_code_becomes_a_rejected_result_with_that_code(string code)
    {
        var rig = NewRig(Connection());
        rig.Handler.Respond = () => EsmsStubHandler.Json($$"""{"CodeResult":"{{code}}","ErrorMessage":"nope"}""");

        var result = await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Equal(code, result.ResultCode);
    }

    [Fact]
    public async Task An_http_error_an_unreadable_answer_and_a_timeout_are_all_rejections()
    {
        var http500 = NewRig(Connection());
        http500.Handler.Respond = () => EsmsStubHandler.Json("oops", HttpStatusCode.InternalServerError);
        Assert.Equal("http-500", (await http500.Sut.SendAsync(Otp(), CancellationToken.None)).ResultCode);

        var garbage = NewRig(Connection());
        garbage.Handler.Respond = () => EsmsStubHandler.Json("<html>not json</html>");
        Assert.Equal("bad-response", (await garbage.Sut.SendAsync(Otp(), CancellationToken.None)).ResultCode);

        var timeout = NewRig(Connection(), new EsmsTimeoutHandler());
        var timedOut = await timeout.Sut.SendAsync(Otp(), CancellationToken.None);
        Assert.False(timedOut.Accepted);
        Assert.Equal("timeout", timedOut.ResultCode);
    }

    [Fact]
    public async Task The_callers_own_cancellation_is_not_swallowed()
    {
        var rig = NewRig(Connection(), new EsmsTimeoutHandler());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.Sut.SendAsync(Otp(), cancelled.Token));
    }

    [Fact]
    public async Task A_missing_or_switched_off_connection_hands_the_message_to_the_fallback_without_calling_eSMS()
    {
        var off = NewRig(Connection(enabled: false));
        var absent = NewRig(connection: null);

        var refused = await off.Sut.SendAsync(Otp(), CancellationToken.None);
        var alsoRefused = await absent.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.Equal(0, off.Handler.Calls);
        Assert.Equal(0, absent.Handler.Calls);
        // Outside Development the fallback must refuse, never pretend a code was sent.
        Assert.False(refused.Accepted);
        Assert.False(alsoRefused.Accepted);
        Assert.Equal("no-provider", refused.ResultCode);
    }

    [Fact]
    public async Task In_development_the_fallback_counts_as_sent_so_a_flow_can_be_finished_locally()
    {
        var rig = NewRig(connection: null, environment: "Development");

        var result = await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("logging", result.Provider);
        Assert.Equal(0, rig.Handler.Calls);
    }

    [Theory]
    [InlineData("oaId")]
    [InlineData("brandname")]
    [InlineData("otpRegisterTemplateId")]
    [InlineData("otpRegisterSmsContent")]
    public async Task An_incomplete_setting_blocks_the_send_and_names_what_is_missing(string missingSetting)
    {
        var settings = new Dictionary<string, string>
        {
            ["oaId"] = "1", ["brandname"] = "B", ["otpRegisterTemplateId"] = "9", ["otpRegisterSmsContent"] = "{code}",
        };
        settings.Remove(missingSetting);
        var rig = NewRig(Connection(settings: settings));

        var result = await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Equal("not-configured", result.ResultCode);
        Assert.Equal(0, rig.Handler.Calls);
        Assert.Contains(rig.Log.Lines, line => line.Contains(missingSetting));
    }

    [Fact]
    public async Task Missing_secrets_block_the_send()
    {
        var rig = NewRig(Connection(secrets: new Dictionary<string, string> { ["apiKey"] = ApiKey }));

        var result = await rig.Sut.SendAsync(Otp(), CancellationToken.None);

        Assert.Equal("not-configured", result.ResultCode);
        Assert.Equal(0, rig.Handler.Calls);
        Assert.Contains(rig.Log.Lines, line => line.Contains("secretKey"));
    }

    [Fact]
    public async Task An_unknown_message_kind_is_rejected_without_calling_eSMS()
    {
        var rig = NewRig(Connection());

        var result = await rig.Sut.SendAsync(Otp(kind: "order-placed"), CancellationToken.None);

        Assert.Equal("unsupported-kind", result.ResultCode);
        Assert.Equal(0, rig.Handler.Calls);
    }

    [Fact]
    public async Task A_missing_template_parameter_blocks_the_send()
    {
        var rig = NewRig(Connection());
        var message = Otp() with { Parameters = new Dictionary<string, string>() };

        var result = await rig.Sut.SendAsync(message, CancellationToken.None);

        Assert.Equal("not-configured", result.ResultCode);
        Assert.Equal(0, rig.Handler.Calls);
    }

    [Fact]
    public async Task Nothing_sensitive_ever_reaches_the_log()
    {
        var failure = NewRig(Connection());
        failure.Handler.Respond = () => EsmsStubHandler.Json("""{"CodeResult":"101","ErrorMessage":"Authorize Failed"}""");
        var unreachable = NewRig(Connection(), new EsmsTimeoutHandler());
        var misconfigured = NewRig(Connection(secrets: new Dictionary<string, string> { ["apiKey"] = ApiKey }));

        await failure.Sut.SendAsync(Otp(), CancellationToken.None);
        await unreachable.Sut.SendAsync(Otp(), CancellationToken.None);
        await misconfigured.Sut.SendAsync(Otp(), CancellationToken.None);

        var everything = string.Join('\n', failure.Log.Lines.Concat(unreachable.Log.Lines).Concat(misconfigured.Log.Lines));
        Assert.NotEmpty(everything);
        Assert.DoesNotContain(ApiKey, everything);
        Assert.DoesNotContain(SecretKey, everything);
        Assert.DoesNotContain(Code, everything);
        Assert.DoesNotContain(Phone, everything);
    }

    [Fact]
    public async Task The_secret_bearing_request_is_never_sent_over_plain_http()
    {
        var rig = NewRig(Connection(), options: new EsmsOptions { Endpoint = "http://rest.esms.vn/MainService.svc/json/MultiChannelMessage/" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => rig.Sut.SendAsync(Otp(), CancellationToken.None));
        Assert.Equal(0, rig.Handler.Calls);
    }

    [Fact]
    public void A_connections_text_form_hides_its_secrets()
    {
        var text = Connection().ToString();

        Assert.DoesNotContain(ApiKey, text);
        Assert.DoesNotContain(SecretKey, text);
    }
}

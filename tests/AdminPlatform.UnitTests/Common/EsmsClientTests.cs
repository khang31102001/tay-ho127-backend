using System.Text.Json;
using AdminPlatform.Common.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Common;

public class EsmsClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly System.Net.HttpStatusCode _status;

        public StubHandler(string responseBody, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _status = status;
        }

        public Uri? RequestUri { get; private set; }

        public JsonElement Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement.Clone();
            return new HttpResponseMessage(_status) { Content = new StringContent(_responseBody) };
        }
    }

    private static EsmsClient NewClient(StubHandler handler, EsmsOptions? options = null) => new(
        new HttpClient(handler),
        Options.Create(options ?? new EsmsOptions { ApiKey = "key", SecretKey = "secret", ZaloOaId = "oa-1" }),
        NullLogger<EsmsClient>.Instance);

    [Fact]
    public async Task SendZaloAsync_posts_the_template_params_and_local_phone_format()
    {
        var handler = new StubHandler("""{"CodeResult":"100"}""");

        await NewClient(handler).SendZaloAsync("+84 901 234 567", "540610", ["123456"], CancellationToken.None);

        Assert.EndsWith("SendZaloMessage_V4_post_json/", handler.RequestUri!.ToString());
        Assert.Equal("0901234567", handler.Body.GetProperty("Phone").GetString());
        Assert.Equal("540610", handler.Body.GetProperty("TempID").GetString());
        Assert.Equal("oa-1", handler.Body.GetProperty("OAID").GetString());
        Assert.Equal("123456", handler.Body.GetProperty("Params")[0].GetString());
        Assert.Equal("key", handler.Body.GetProperty("ApiKey").GetString());
    }

    [Fact]
    public async Task SendSmsAsync_posts_the_content()
    {
        var handler = new StubHandler("""{"CodeResult":"100"}""");

        await NewClient(handler).SendSmsAsync("0901234567", "123456 la ma", CancellationToken.None);

        Assert.EndsWith("SendMultipleMessage_V4_post_json/", handler.RequestUri!.ToString());
        Assert.Equal("123456 la ma", handler.Body.GetProperty("Content").GetString());
        Assert.False(handler.Body.TryGetProperty("Brandname", out _));
    }

    [Theory]
    [InlineData("""{"CodeResult":"103","ErrorMessage":"insufficient balance"}""")]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task A_response_that_is_not_accepted_is_a_failure(string responseBody)
    {
        var client = NewClient(new StubHandler(responseBody));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendZaloAsync("0901234567", "540610", ["1"], CancellationToken.None));
    }

    [Fact]
    public async Task An_http_error_is_a_failure()
    {
        var client = NewClient(new StubHandler("oops", System.Net.HttpStatusCode.BadGateway));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendSmsAsync("0901234567", "x", CancellationToken.None));
    }

    [Fact]
    public async Task SendZaloAsync_needs_an_official_account_and_a_template()
    {
        var client = NewClient(new StubHandler("""{"CodeResult":"100"}"""), new EsmsOptions { ApiKey = "key", SecretKey = "secret" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendZaloAsync("0901234567", "540610", ["1"], CancellationToken.None));
    }

    [Theory]
    [InlineData("0901234567", "0901234567")]
    [InlineData("+84901234567", "0901234567")]
    [InlineData("84901234567", "0901234567")]
    [InlineData("090 123 4567", "0901234567")]
    public void NormalizePhone_produces_the_local_form(string input, string expected) =>
        Assert.Equal(expected, EsmsClient.NormalizePhone(input));
}

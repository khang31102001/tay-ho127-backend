using AdminPlatform.Common.Messaging;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Sales;

public class ZaloOrderNotifierTests
{
    private sealed class RecordingEsmsClient : IEsmsClient
    {
        public List<(string Phone, string TemplateId, IReadOnlyList<string> Params)> ZaloMessages { get; } = [];

        public Exception? Failure { get; set; }

        public Task SendZaloAsync(string phone, string templateId, IReadOnlyList<string> templateParams, CancellationToken cancellationToken)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            ZaloMessages.Add((phone, templateId, templateParams));
            return Task.CompletedTask;
        }

        public Task SendSmsAsync(string phone, string content, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static readonly OrderNotification Order = new(Guid.NewGuid(), "TH-20261014-0007", "Nguyễn Văn A", "0901234567", "a@example.com", 125000.4m);

    private static ZaloOrderNotifier NewSut(RecordingEsmsClient esms, string[]? paramNames = null) => new(
        esms,
        Options.Create(new EsmsOptions
        {
            ZaloOrderPlacedTemplateId = "540646",
            ZaloOrderPlacedParams = paramNames ?? ["CustomerName", "OrderCode", "TotalAmount"],
        }),
        NullLogger<ZaloOrderNotifier>.Instance);

    [Fact]
    public async Task OrderPlacedAsync_sends_the_order_template_with_params_in_the_configured_order()
    {
        var esms = new RecordingEsmsClient();

        await NewSut(esms).OrderPlacedAsync(Order, CancellationToken.None);

        var message = Assert.Single(esms.ZaloMessages);
        Assert.Equal("0901234567", message.Phone);
        Assert.Equal("540646", message.TemplateId);
        Assert.Equal(["Nguyễn Văn A", "TH-20261014-0007", "125000"], message.Params);
    }

    [Fact]
    public async Task OrderPlacedAsync_follows_a_different_param_order()
    {
        var esms = new RecordingEsmsClient();

        await NewSut(esms, ["OrderCode", "Phone"]).OrderPlacedAsync(Order, CancellationToken.None);

        Assert.Equal(["TH-20261014-0007", "0901234567"], esms.ZaloMessages[0].Params);
    }

    [Fact]
    public async Task OrderPlacedAsync_never_throws_when_delivery_fails()
    {
        var esms = new RecordingEsmsClient { Failure = new InvalidOperationException("number has no Zalo") };

        await NewSut(esms).OrderPlacedAsync(Order, CancellationToken.None);

        Assert.Empty(esms.ZaloMessages);
    }

    [Fact]
    public async Task OrderPlacedAsync_swallows_a_misconfigured_param_name_instead_of_failing_the_order()
    {
        var esms = new RecordingEsmsClient();

        await NewSut(esms, ["NoSuchValue"]).OrderPlacedAsync(Order, CancellationToken.None);

        Assert.Empty(esms.ZaloMessages);
    }

    [Fact]
    public async Task OrderPlacedAsync_still_honours_the_callers_cancellation()
    {
        var esms = new RecordingEsmsClient { Failure = new OperationCanceledException() };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => NewSut(esms).OrderPlacedAsync(Order, cts.Token));
    }
}

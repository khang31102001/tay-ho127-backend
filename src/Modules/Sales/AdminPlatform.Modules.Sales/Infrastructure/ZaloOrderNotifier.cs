using System.Globalization;
using AdminPlatform.Common.Messaging;
using AdminPlatform.Modules.Sales.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Sales.Infrastructure;

/// <summary>Tells the customer on Zalo (ZNS through eSMS) that their order was placed. The message content is the
/// approved template in Esms:ZaloOrderPlacedTemplateId, filled with the values named in Esms:ZaloOrderPlacedParams.
/// Delivery is best effort: the order is already saved, so a failure (the number has no Zalo, eSMS is down) is
/// logged and never reaches the caller. Status changes are not messaged yet.</summary>
internal sealed class ZaloOrderNotifier : IOrderNotifier
{
    /// <summary>The order request waits for this call, so it must not hold the customer up for long.</summary>
    internal static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(8);

    private readonly IEsmsClient _esms;
    private readonly EsmsOptions _options;
    private readonly ILogger<ZaloOrderNotifier> _logger;

    public ZaloOrderNotifier(IEsmsClient esms, IOptions<EsmsOptions> options, ILogger<ZaloOrderNotifier> logger)
    {
        _esms = esms;
        _options = options.Value;
        _logger = logger;
    }

    public async Task OrderPlacedAsync(OrderNotification notification, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SendTimeout);

        try
        {
            await _esms.SendZaloAsync(notification.Phone, _options.ZaloOrderPlacedTemplateId, BuildParams(notification), timeout.Token);
            _logger.LogInformation("Order {OrderCode}: Zalo confirmation sent.", notification.OrderCode);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Order {OrderCode}: Zalo confirmation was not delivered.", notification.OrderCode);
        }
    }

    public Task OrderStatusChangedAsync(OrderNotification notification, string fromStatus, string toStatus, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderCode} moved {FromStatus} -> {ToStatus}.", notification.OrderCode, fromStatus, toStatus);
        return Task.CompletedTask;
    }

    private List<string> BuildParams(OrderNotification notification) =>
        _options.ZaloOrderPlacedParams.Select(name => ValueOf(name, notification)).ToList();

    private static string ValueOf(string name, OrderNotification notification) => name.Trim().ToLowerInvariant() switch
    {
        "customername" => notification.CustomerName,
        "ordercode" => notification.OrderCode,
        "phone" => notification.Phone,
        "totalamount" => decimal.Round(notification.TotalAmount, 0).ToString("0", CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Unknown Esms:ZaloOrderPlacedParams entry '{name}'."),
    };
}

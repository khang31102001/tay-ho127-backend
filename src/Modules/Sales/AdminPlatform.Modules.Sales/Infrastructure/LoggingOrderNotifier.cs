using AdminPlatform.Modules.Sales.Application.Ports;
using Microsoft.Extensions.Logging;

namespace AdminPlatform.Modules.Sales.Infrastructure;

/// <summary>The default notifier: it only logs. Replace it by registering another IOrderNotifier (email, SMS,
/// push...) once a provider exists — nothing else in Sales needs to change. Never logs the phone or email.</summary>
internal sealed class LoggingOrderNotifier : IOrderNotifier
{
    private readonly ILogger<LoggingOrderNotifier> _logger;

    public LoggingOrderNotifier(ILogger<LoggingOrderNotifier> logger)
    {
        _logger = logger;
    }

    public Task OrderPlacedAsync(OrderNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderCode} placed, total {TotalAmount}.", notification.OrderCode, notification.TotalAmount);
        return Task.CompletedTask;
    }

    public Task OrderStatusChangedAsync(OrderNotification notification, string fromStatus, string toStatus, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderCode} moved {FromStatus} -> {ToStatus}.", notification.OrderCode, fromStatus, toStatus);
        return Task.CompletedTask;
    }
}

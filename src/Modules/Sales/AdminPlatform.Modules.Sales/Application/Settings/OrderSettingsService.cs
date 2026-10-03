using AdminPlatform.Modules.Sales.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.Settings;

/// <param name="OrderCodePrefix">Letters/digits, upper-cased: the start of every order code ("TH127").</param>
/// <param name="OrderCodeDateFormat">"yyMMdd", "yyyyMMdd", "yyMM" or "none". The running number restarts whenever the date part changes.</param>
/// <param name="OrderCodeSequenceLength">Zero-padded width of the running number (3-8).</param>
/// <param name="PaymentSessionMinutes">How long a QR/wallet payment session holds the order (5-240).</param>
public sealed record UpdateOrderSettingsRequest(string OrderCodePrefix, string OrderCodeDateFormat, int OrderCodeSequenceLength, int PaymentSessionMinutes);

public sealed record OrderSettingsResponse(
    string OrderCodePrefix,
    string OrderCodeDateFormat,
    int OrderCodeSequenceLength,
    int PaymentSessionMinutes,
    string ExampleOrderCode,
    DateTime UpdatedAt);

public interface IOrderSettingsService
{
    /// <summary>The single settings row; created with the defaults the first time anything asks.</summary>
    Task<OrderSettingsResponse> GetAsync(CancellationToken cancellationToken);

    Task<OrderSettingsResponse> UpdateAsync(UpdateOrderSettingsRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateOrderSettingsRequestValidator : AbstractValidator<UpdateOrderSettingsRequest>
{
    public UpdateOrderSettingsRequestValidator()
    {
        RuleFor(x => x.OrderCodePrefix).NotEmpty().MaximumLength(OrderSettings.MaxPrefixLength);
        RuleFor(x => x.OrderCodeDateFormat).Must(value => OrderSettings.AllowedDateFormats.Contains(value))
            .WithMessage($"The date format must be one of: {string.Join(", ", OrderSettings.AllowedDateFormats)}.");
        RuleFor(x => x.OrderCodeSequenceLength).InclusiveBetween(OrderSettings.MinSequenceLength, OrderSettings.MaxSequenceLength);
        RuleFor(x => x.PaymentSessionMinutes).InclusiveBetween(OrderSettings.MinSessionMinutes, OrderSettings.MaxSessionMinutes);
    }
}

public sealed class OrderSettingsService : IOrderSettingsService
{
    private readonly ISalesDbContext _db;

    public OrderSettingsService(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<OrderSettingsResponse> GetAsync(CancellationToken cancellationToken) =>
        ToResponse(await OrderSettingsStore.GetOrCreateAsync(_db, cancellationToken));

    public async Task<OrderSettingsResponse> UpdateAsync(UpdateOrderSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await OrderSettingsStore.GetOrCreateAsync(_db, cancellationToken);
        settings.Update(new OrderSettingsDetails(
            request.OrderCodePrefix, request.OrderCodeDateFormat, request.OrderCodeSequenceLength, request.PaymentSessionMinutes));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    private static OrderSettingsResponse ToResponse(OrderSettings s) => new(
        s.OrderCodePrefix,
        s.OrderCodeDateFormat,
        s.OrderCodeSequenceLength,
        s.PaymentSessionMinutes,
        s.BuildCode(VietnamTime.Now(DateTime.UtcNow), 128),
        s.UpdatedAtUtc ?? s.CreatedAtUtc);
}

/// <summary>Reads the one settings row, creating it with the defaults when it does not exist yet — so the
/// system works on a database that was never seeded.</summary>
internal static class OrderSettingsStore
{
    public static async Task<OrderSettings> GetOrCreateAsync(ISalesDbContext db, CancellationToken cancellationToken)
    {
        var settings = await db.OrderSettings.OrderBy(s => s.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = OrderSettings.Create(new OrderSettingsDetails("TH127", "yyMMdd", 5, 15));
        db.OrderSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }
}

/// <summary>The date on an order code is the restaurant's own (Vietnam, UTC+7, no daylight saving) — a fixed
/// offset, because slim container images often lack time-zone data.</summary>
internal static class VietnamTime
{
    public static DateTime Now(DateTime utcNow) => utcNow.AddHours(7);
}

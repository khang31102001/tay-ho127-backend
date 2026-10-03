using System.Globalization;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>Order-wide settings (one row): how an order code is built and how long a payment session holds
/// an order. Kept in a table so the restaurant changes them without a deployment, and so more order-wide
/// settings can be added here later.</summary>
public sealed class OrderSettings : AuditableEntity
{
    public const int MaxPrefixLength = 16;
    public const int MinSequenceLength = 3;
    public const int MaxSequenceLength = 8;
    public const int MinSessionMinutes = 5;
    public const int MaxSessionMinutes = 240;

    /// <summary>Date parts an order code may carry; the sequence number restarts whenever the part changes.</summary>
    public static readonly IReadOnlyList<string> AllowedDateFormats = ["yyMMdd", "yyyyMMdd", "yyMM", "none"];

    /// <summary>Text the order code starts with, e.g. "TH127". Letters and digits only.</summary>
    public string OrderCodePrefix { get; private set; } = "TH127";

    /// <summary>One of <see cref="AllowedDateFormats"/>; "none" means the code has no date part.</summary>
    public string OrderCodeDateFormat { get; private set; } = "yyMMdd";

    /// <summary>Zero-padded width of the running number ("00128" = 5).</summary>
    public int OrderCodeSequenceLength { get; private set; } = 5;

    /// <summary>How long a QR / wallet payment session holds the order before it expires.</summary>
    public int PaymentSessionMinutes { get; private set; } = 15;

    private OrderSettings()
    {
        // EF Core
    }

    public static OrderSettings Create(OrderSettingsDetails details)
    {
        var settings = new OrderSettings { Id = Guid.NewGuid() };
        settings.Update(details);
        return settings;
    }

    public void Update(OrderSettingsDetails details)
    {
        var prefix = (details.OrderCodePrefix ?? string.Empty).Trim().ToUpperInvariant();
        if (prefix.Length is 0 or > MaxPrefixLength || !prefix.All(char.IsAsciiLetterOrDigit))
        {
            throw new BusinessRuleValidationException($"The order code prefix must be 1-{MaxPrefixLength} letters or digits.");
        }

        if (!AllowedDateFormats.Contains(details.OrderCodeDateFormat))
        {
            throw new BusinessRuleValidationException($"The order code date format must be one of: {string.Join(", ", AllowedDateFormats)}.");
        }

        if (details.OrderCodeSequenceLength is < MinSequenceLength or > MaxSequenceLength)
        {
            throw new BusinessRuleValidationException($"The sequence length must be {MinSequenceLength}-{MaxSequenceLength}.");
        }

        if (details.PaymentSessionMinutes is < MinSessionMinutes or > MaxSessionMinutes)
        {
            throw new BusinessRuleValidationException($"A payment session must last {MinSessionMinutes}-{MaxSessionMinutes} minutes.");
        }

        OrderCodePrefix = prefix;
        OrderCodeDateFormat = details.OrderCodeDateFormat;
        OrderCodeSequenceLength = details.OrderCodeSequenceLength;
        PaymentSessionMinutes = details.PaymentSessionMinutes;
    }

    /// <summary>The counter bucket a new code draws its number from: prefix + date part. A new bucket
    /// (a new day, or a new prefix) restarts the numbering at 1.</summary>
    public string CounterKey(DateTime localNow) =>
        OrderCodeDateFormat == "none" ? OrderCodePrefix : $"{OrderCodePrefix}-{DatePart(localNow)}";

    public string BuildCode(DateTime localNow, int sequence)
    {
        var number = sequence.ToString(CultureInfo.InvariantCulture).PadLeft(OrderCodeSequenceLength, '0');
        return OrderCodeDateFormat == "none" ? $"{OrderCodePrefix}-{number}" : $"{OrderCodePrefix}-{DatePart(localNow)}-{number}";
    }

    private string DatePart(DateTime localNow) => localNow.ToString(OrderCodeDateFormat, CultureInfo.InvariantCulture);
}

public sealed record OrderSettingsDetails(string OrderCodePrefix, string OrderCodeDateFormat, int OrderCodeSequenceLength, int PaymentSessionMinutes);

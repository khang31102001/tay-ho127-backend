using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

public enum DeliveryMethodType
{
    Delivery,
    Pickup,
}

/// <summary>A way the customer gets the order (delivered or picked up), with its fee. Orders look the fee
/// up from here at order time — the browser never states it. <see cref="Code"/> is the stable identity
/// the site and orders refer to, so it cannot change after creation.</summary>
public sealed partial class DeliveryMethod : AuditableEntity
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 200;
    public const int MaxTextLength = 2000;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DeliveryMethodType Type { get; private set; }
    public decimal BaseFee { get; private set; }

    /// <summary>An order subtotal at or above this waives <see cref="BaseFee"/>; null = never waived.</summary>
    public decimal? FreeShippingThreshold { get; private set; }

    public int? EstimatedMinMinutes { get; private set; }
    public int? EstimatedMaxMinutes { get; private set; }

    /// <summary>Only meaningful (and kept) for a pickup method.</summary>
    public string? PickupAddress { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>At most one method is the default; the service clears the flag on the others.</summary>
    public bool IsDefault { get; private set; }

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]*$")]
    private static partial Regex CodeRegex();

    private DeliveryMethod()
    {
        // EF Core
    }

    public static bool IsValidCode(string? code) =>
        code is { Length: > 0 and <= MaxCodeLength } && CodeRegex().IsMatch(code);

    public static DeliveryMethod Create(string code, DeliveryMethodDetails details)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsValidCode(normalized))
        {
            throw new BusinessRuleValidationException($"'{code}' is not a valid code (lowercase letters, digits, '-' and '_').");
        }

        var method = new DeliveryMethod { Id = Guid.NewGuid(), Code = normalized };
        method.Update(details);
        return method;
    }

    public void Update(DeliveryMethodDetails details)
    {
        if (details.BaseFee < 0 || details.FreeShippingThreshold < 0)
        {
            throw new BusinessRuleValidationException("Amounts cannot be negative.");
        }

        if (details.EstimatedMinMinutes < 0 || details.EstimatedMaxMinutes < 0)
        {
            throw new BusinessRuleValidationException("Estimated minutes cannot be negative.");
        }

        if (details.EstimatedMinMinutes > details.EstimatedMaxMinutes)
        {
            throw new BusinessRuleValidationException("The minimum estimated time cannot exceed the maximum.");
        }

        if (details.Type == DeliveryMethodType.Pickup && string.IsNullOrWhiteSpace(details.PickupAddress))
        {
            throw new BusinessRuleValidationException("A pickup method needs a pickup address.");
        }

        Name = Guard.NotNullOrWhiteSpace(details.Name, nameof(details.Name)).Trim();
        Description = Clean(details.Description);
        Type = details.Type;
        BaseFee = details.BaseFee;
        FreeShippingThreshold = details.FreeShippingThreshold;
        EstimatedMinMinutes = details.EstimatedMinMinutes;
        EstimatedMaxMinutes = details.EstimatedMaxMinutes;
        PickupAddress = details.Type == DeliveryMethodType.Pickup ? Clean(details.PickupAddress) : null;
        DisplayOrder = details.DisplayOrder;
        IsActive = details.IsActive;
        IsDefault = details.IsDefault;
    }

    public void ClearDefault() => IsDefault = false;

    /// <summary>The fee for an order with this subtotal — the single rule shared by every caller.</summary>
    public decimal ResolveFee(decimal subtotal) =>
        FreeShippingThreshold is { } threshold && subtotal >= threshold ? 0 : BaseFee;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record DeliveryMethodDetails(
    string Name,
    string? Description,
    DeliveryMethodType Type,
    decimal BaseFee,
    decimal? FreeShippingThreshold,
    int? EstimatedMinMinutes,
    int? EstimatedMaxMinutes,
    string? PickupAddress,
    int DisplayOrder,
    bool IsActive,
    bool IsDefault);

using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

public enum PaymentMethodGroup
{
    Cod,
    Card,
    BankTransfer,
    EWallet,
}

/// <summary>How the customer can pay at checkout (not a payment that happened — that is <see cref="Payment"/>).
/// <see cref="Code"/> is the stable identity orders refer to. A COD method is paid on delivery; every other
/// group is paid up front through a <see cref="PaymentSession"/>.</summary>
public sealed partial class PaymentMethod : AuditableEntity
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 200;
    public const int MaxTextLength = 2000;
    public const int MaxShortTextLength = 200;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Opaque Media id — Sales never reads the Media module.</summary>
    public string? IconMediaId { get; private set; }

    public PaymentMethodGroup Group { get; private set; }

    /// <summary>The payment provider's name for a card / e-wallet method ("vnpay", "momo"...). Internal.</summary>
    public string? Gateway { get; private set; }

    public string? Instructions { get; private set; }
    public string? BankName { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankAccountHolder { get; private set; }
    public string? BankBranch { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxOrderAmount { get; private set; }

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]*$")]
    private static partial Regex CodeRegex();

    private PaymentMethod()
    {
        // EF Core
    }

    public static bool IsValidCode(string? code) =>
        code is { Length: > 0 and <= MaxCodeLength } && CodeRegex().IsMatch(code);

    public static PaymentMethod Create(string code, PaymentMethodDetails details)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsValidCode(normalized))
        {
            throw new BusinessRuleValidationException($"'{code}' is not a valid code (lowercase letters, digits, '-' and '_').");
        }

        var method = new PaymentMethod { Id = Guid.NewGuid(), Code = normalized };
        method.Update(details);
        return method;
    }

    public void Update(PaymentMethodDetails details)
    {
        if (details.MinOrderAmount < 0 || details.MaxOrderAmount < 0)
        {
            throw new BusinessRuleValidationException("Amounts cannot be negative.");
        }

        if (details.MinOrderAmount > details.MaxOrderAmount)
        {
            throw new BusinessRuleValidationException("The minimum order amount cannot exceed the maximum.");
        }

        Name = Guard.NotNullOrWhiteSpace(details.Name, nameof(details.Name)).Trim();
        Description = Clean(details.Description);
        IconMediaId = Clean(details.IconMediaId);
        Group = details.Group;
        Gateway = Clean(details.Gateway);
        Instructions = Clean(details.Instructions);
        BankName = Clean(details.BankName);
        BankAccountNumber = Clean(details.BankAccountNumber);
        BankAccountHolder = Clean(details.BankAccountHolder);
        BankBranch = Clean(details.BankBranch);
        DisplayOrder = details.DisplayOrder;
        IsActive = details.IsActive;
        IsDefault = details.IsDefault;
        MinOrderAmount = details.MinOrderAmount;
        MaxOrderAmount = details.MaxOrderAmount;
    }

    public void ClearDefault() => IsDefault = false;

    /// <summary>true for a method paid on delivery — the only kind an order can be placed with directly.</summary>
    public bool IsPaidOnDelivery => Group == PaymentMethodGroup.Cod;

    /// <summary>Whether an order with this subtotal may use the method (its min/max order amount).</summary>
    public bool IsEligible(decimal subtotal) =>
        (MinOrderAmount is not { } min || subtotal >= min) && (MaxOrderAmount is not { } max || subtotal <= max);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record PaymentMethodDetails(
    string Name,
    string? Description,
    string? IconMediaId,
    PaymentMethodGroup Group,
    string? Gateway,
    string? Instructions,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountHolder,
    string? BankBranch,
    int DisplayOrder,
    bool IsActive,
    bool IsDefault,
    decimal? MinOrderAmount,
    decimal? MaxOrderAmount);

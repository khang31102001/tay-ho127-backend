namespace AdminPlatform.Modules.Sales.Application.PaymentMethods;

/// <summary>Code: lowercase letters, digits, '-' and '_' — unique, immutable. Group: "cod" | "card" |
/// "bank_transfer" | "e_wallet". A "cod" method is paid on delivery; every other group is paid up front through
/// a payment session. MinOrderAmount/MaxOrderAmount restrict which order subtotals may use the method.
/// At most one method is the default.</summary>
public sealed record CreatePaymentMethodRequest(
    string Code,
    string Name,
    string? Description,
    string? IconMediaId,
    string Group,
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

public sealed record UpdatePaymentMethodRequest(
    string Name,
    string? Description,
    string? IconMediaId,
    string Group,
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

public sealed record PaymentMethodResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? IconMediaId,
    string Group,
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
    decimal? MaxOrderAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>What the checkout shows — only active methods. The gateway name and the bank account stay out:
/// the bank details reach the customer only inside their own payment session.</summary>
public sealed record PublicPaymentMethodResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? IconMediaId,
    string Group,
    string? Instructions,
    int DisplayOrder,
    bool IsDefault,
    decimal? MinOrderAmount,
    decimal? MaxOrderAmount);

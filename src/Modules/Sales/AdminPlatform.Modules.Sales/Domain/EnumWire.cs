using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>Maps an enum to/from the lowercase snake_case text used on the wire and in the database
/// (BankTransfer ⇄ "bank_transfer", EWallet ⇄ "e_wallet"), so the three layers never disagree on spelling.</summary>
public static partial class EnumWire
{
    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex WordBoundary();

    public static string ToWire<T>(T value) where T : struct, Enum =>
        WordBoundary().Replace(value.ToString(), "_").ToLowerInvariant();

    public static bool TryParse<T>(string? text, out T value) where T : struct, Enum
    {
        var candidate = text?.Trim();
        foreach (var option in Enum.GetValues<T>())
        {
            if (string.Equals(ToWire(option), candidate, StringComparison.OrdinalIgnoreCase))
            {
                value = option;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static T Parse<T>(string? text, string fieldName) where T : struct, Enum =>
        TryParse<T>(text, out var value)
            ? value
            : throw new BusinessRuleValidationException($"'{text}' is not a valid {fieldName}.");
}

using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>A Vietnamese phone number in its one stored form ("0901234567"): "+84 901.234.567" and
/// "0901 234 567" are the same number, which matters because the phone is the key a guest uses to look an
/// order up again.</summary>
public static partial class PhoneNumber
{
    [GeneratedRegex(@"^0\d{9}$")]
    private static partial Regex Canonical();

    public static string Normalize(string? value)
    {
        var text = Guard.NotNullOrWhiteSpace(value, "Phone");
        var compact = new string(text.Where(c => !char.IsWhiteSpace(c) && c is not ('.' or '-' or '(' or ')')).ToArray());
        if (compact.StartsWith("+84", StringComparison.Ordinal))
        {
            compact = "0" + compact[3..];
        }

        return Canonical().IsMatch(compact)
            ? compact
            : throw new BusinessRuleValidationException("The phone number is not a valid Vietnamese phone number.");
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        try
        {
            normalized = Normalize(value);
            return true;
        }
        catch (BusinessRuleValidationException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}

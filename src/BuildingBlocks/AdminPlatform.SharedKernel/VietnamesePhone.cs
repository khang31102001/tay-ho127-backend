using System.Text.RegularExpressions;

namespace AdminPlatform.SharedKernel;

/// <summary>A Vietnamese phone number in its one stored form ("0901234567"): "+84 901.234.567" and "0901 234 567" are
/// the same number. Shared by every module that stores or compares a phone (Sales orders, Customer accounts, one-time
/// codes), so they all agree on what "the same number" means.</summary>
public static partial class VietnamesePhone
{
    [GeneratedRegex(@"^0\d{9}$")]
    private static partial Regex Canonical();

    /// <summary>The canonical form, or an exception when the text is blank or not a Vietnamese number.</summary>
    public static string Normalize(string? value)
    {
        var text = Guard.NotNullOrWhiteSpace(value, "Phone");
        var compact = Compact(text);
        return Canonical().IsMatch(compact)
            ? compact
            : throw new BusinessRuleValidationException("The phone number is not a valid Vietnamese phone number.");
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var compact = Compact(value);
            if (Canonical().IsMatch(compact))
            {
                normalized = compact;
                return true;
            }
        }

        normalized = string.Empty;
        return false;
    }

    /// <summary>For data that may legitimately hold something that is not a Vietnamese mobile number (older records, an
    /// admin typing a foreign number): the canonical form when there is one, otherwise the trimmed text; null when blank.
    /// Use <see cref="Normalize"/> wherever the number must be valid (e.g. before sending a code to it).</summary>
    public static string? NormalizeLenient(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryNormalize(value, out var normalized) ? normalized : value.Trim();
    }

    private static string Compact(string text)
    {
        var compact = new string(text.Where(c => !char.IsWhiteSpace(c) && c is not ('.' or '-' or '(' or ')')).ToArray());
        return compact.StartsWith("+84", StringComparison.Ordinal) ? "0" + compact[3..] : compact;
    }
}

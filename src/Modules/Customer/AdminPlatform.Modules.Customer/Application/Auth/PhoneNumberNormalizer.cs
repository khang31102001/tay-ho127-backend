using System.Text.RegularExpressions;

namespace AdminPlatform.Modules.Customer.Application.Auth;

/// <summary>One shape for a Vietnamese mobile number (0xxxxxxxxx) so "+84 901 234 567" and "0901234567" are the
/// same phone for uniqueness, code lookup and the provider.</summary>
public static partial class PhoneNumberNormalizer
{
    [GeneratedRegex(@"^0\d{9,10}$")]
    private static partial Regex LocalFormat();

    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (digits.StartsWith("+84", StringComparison.Ordinal))
        {
            digits = "0" + digits[3..];
        }
        else if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length is 11 or 12)
        {
            digits = "0" + digits[2..];
        }

        return digits;
    }

    public static bool IsValid(string? phone) => LocalFormat().IsMatch(Normalize(phone));
}

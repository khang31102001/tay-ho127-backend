using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>A Vietnamese phone number in its one stored form ("0901234567"): "+84 901.234.567" and
/// "0901 234 567" are the same number, which matters because the phone is the key a guest uses to look an
/// order up again. The rule itself lives in SharedKernel (<see cref="VietnamesePhone"/>) so Customer accounts
/// and one-time codes agree with orders on what "the same number" means.</summary>
public static class PhoneNumber
{
    public static string Normalize(string? value) => VietnamesePhone.Normalize(value);

    public static bool TryNormalize(string? value, out string normalized) => VietnamesePhone.TryNormalize(value, out normalized);
}

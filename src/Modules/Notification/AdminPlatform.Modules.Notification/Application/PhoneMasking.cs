namespace AdminPlatform.Modules.Notification.Application;

/// <summary>The only form of a phone number allowed in a log or a delivery record.</summary>
internal static class PhoneMasking
{
    /// <summary>"0901234567" becomes "090****567". Anything too short to mask safely is hidden entirely.</summary>
    public static string Mask(string phone)
    {
        var trimmed = phone.Trim();
        return trimmed.Length < 7
            ? "****"
            : string.Concat(trimmed.AsSpan(0, 3), new string('*', trimmed.Length - 6), trimmed.AsSpan(trimmed.Length - 3));
    }
}

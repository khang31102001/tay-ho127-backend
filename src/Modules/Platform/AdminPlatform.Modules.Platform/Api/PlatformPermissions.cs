namespace AdminPlatform.Modules.Platform.Api;

public static class PlatformPermissions
{
    public const string FiscalYearsView = "fiscal-years.view";
    public const string FiscalYearsCreate = "fiscal-years.create";
    public const string FiscalYearsUpdate = "fiscal-years.update";

    public const string SystemSettingsView = "system-settings.view";
    public const string SystemSettingsCreate = "system-settings.create";
    public const string SystemSettingsUpdate = "system-settings.update";
    public const string SystemSettingsDelete = "system-settings.delete";

    public const string AuditLogsView = "audit-logs.view";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (FiscalYearsView, "View fiscal years"),
        (FiscalYearsCreate, "Create fiscal years"),
        (FiscalYearsUpdate, "Update fiscal years"),
        (SystemSettingsView, "View system settings"),
        (SystemSettingsCreate, "Create system settings"),
        (SystemSettingsUpdate, "Update system settings"),
        (SystemSettingsDelete, "Delete system settings"),
        (AuditLogsView, "View audit logs"),
    ];
}

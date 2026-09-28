using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Platform.Infrastructure;

/// <summary>Idempotent base data: one FiscalYear for the sample Organization (id supplied by the
/// Migrator, which seeds Organization first) and the global system settings the application expects —
/// all upserted by Code; an existing setting's value is never overwritten.</summary>
public static class PlatformSeeder
{
    public const string SampleFiscalYearCode = "fy-current";

    private static readonly (string Code, string Name, string Value)[] GlobalSettings =
    [
        ("site.name", "Site name", "Tay Ho 127"),
        ("site.default-locale", "Default locale", "vi-VN"),
        ("site.default-time-zone", "Default time zone", "Asia/Ho_Chi_Minh"),
        ("paging.default-page-size", "Default page size", "20"),
    ];

    public static async Task SeedAsync(IServiceProvider services, Guid sampleOrganizationId, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IPlatformDbContext>();

        var exists = await db.FiscalYears.AnyAsync(
            f => f.OrganizationId == sampleOrganizationId && f.Code == SampleFiscalYearCode, cancellationToken);
        if (!exists)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var yearStart = new DateOnly(today.Year, 1, 1);
            var yearEnd = new DateOnly(today.Year, 12, 31);
            db.FiscalYears.Add(FiscalYear.Create(sampleOrganizationId, SampleFiscalYearCode, $"FY{today.Year}", yearStart, yearEnd));
        }

        var existingSettingCodes = await db.SystemSettings
            .Where(s => s.OrganizationId == null)
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);
        foreach (var (code, name, value) in GlobalSettings)
        {
            if (!existingSettingCodes.Contains(code))
            {
                db.SystemSettings.Add(SystemSetting.Create(code, name, value, organizationId: null));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

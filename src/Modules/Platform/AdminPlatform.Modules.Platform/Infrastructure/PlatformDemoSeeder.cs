using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Platform.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only): the previous fiscal year for the sample
/// Organization, so screens that list/switch fiscal years have more than one entry. Upserted by Code.</summary>
public static class PlatformDemoSeeder
{
    public const string PreviousFiscalYearCode = "fy-previous";

    public static async Task SeedAsync(IServiceProvider services, Guid sampleOrganizationId, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IPlatformDbContext>();

        var exists = await db.FiscalYears.AnyAsync(
            f => f.OrganizationId == sampleOrganizationId && f.Code == PreviousFiscalYearCode, cancellationToken);
        if (exists)
        {
            return;
        }

        var previousYear = DateTime.UtcNow.Year - 1;
        db.FiscalYears.Add(FiscalYear.Create(
            sampleOrganizationId,
            PreviousFiscalYearCode,
            $"FY{previousYear}",
            new DateOnly(previousYear, 1, 1),
            new DateOnly(previousYear, 12, 31)));
        await db.SaveChangesAsync(cancellationToken);
    }
}

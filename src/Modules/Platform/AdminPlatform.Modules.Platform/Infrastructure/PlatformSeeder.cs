using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Platform.Infrastructure;

/// <summary>Idempotent sample data: one FiscalYear for the sample Organization (id supplied by the
/// Migrator, which seeds Organization first) — upserted by Code.</summary>
public static class PlatformSeeder
{
    public const string SampleFiscalYearCode = "fy-current";

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
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

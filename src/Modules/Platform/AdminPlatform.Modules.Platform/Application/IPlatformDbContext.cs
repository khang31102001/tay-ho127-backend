using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Platform.Application;

public interface IPlatformDbContext
{
    DbSet<FiscalYear> FiscalYears { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

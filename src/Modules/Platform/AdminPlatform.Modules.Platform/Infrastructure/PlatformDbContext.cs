using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Platform.Infrastructure;

public sealed class PlatformDbContext : DbContext, IPlatformDbContext
{
    public const string Schema = "platform";

    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}

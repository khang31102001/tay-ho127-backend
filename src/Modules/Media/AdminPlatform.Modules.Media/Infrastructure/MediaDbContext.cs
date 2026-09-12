using AdminPlatform.Modules.Media.Application;
using Microsoft.EntityFrameworkCore;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.Modules.Media.Infrastructure;

public sealed class MediaDbContext : DbContext, IMediaDbContext
{
    public const string Schema = "media";

    public DbSet<MediaEntity> Media => Set<MediaEntity>();

    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Media.Application;

/// <summary>Persistence port for the Media module — Application depends on this, not on EF Core directly.
/// Implemented by MediaDbContext (Infrastructure).</summary>
public interface IMediaDbContext
{
    DbSet<Domain.Media> Media { get; }

    DbSet<Domain.MediaCategory> MediaCategories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

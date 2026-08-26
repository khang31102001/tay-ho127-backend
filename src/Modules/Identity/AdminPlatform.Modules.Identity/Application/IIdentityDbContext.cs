using AdminPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Identity.Application;

/// <summary>Persistence port for the Identity module — the Application layer depends on this, not on EF
/// Core directly. Implemented by IdentityDbContext (Infrastructure). Not a generic repository: it exposes
/// exactly the DbSets this module needs, per architecture assumption #4.</summary>
public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

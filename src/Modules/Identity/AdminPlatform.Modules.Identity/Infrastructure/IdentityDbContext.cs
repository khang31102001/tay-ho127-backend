using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Identity.Infrastructure;

public sealed class IdentityDbContext : DbContext, IIdentityDbContext
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}

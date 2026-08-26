using AdminPlatform.Modules.Identity.Application;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Identity.Infrastructure;

internal sealed class UserLookupService : IUserLookupService
{
    private readonly IIdentityDbContext _db;

    public UserLookupService(IIdentityDbContext db)
    {
        _db = db;
    }

    public async Task<UserLookupResult?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserLookupResult(u.Id, u.Email, u.FullName, u.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserLookupResult?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.Users
            .Where(u => u.Email == normalized)
            .Select(u => new UserLookupResult(u.Id, u.Email, u.FullName, u.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

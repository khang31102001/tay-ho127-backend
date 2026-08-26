namespace AdminPlatform.Modules.Identity.Application;

public sealed record UserLookupResult(Guid Id, string Email, string FullName, bool IsActive);

/// <summary>The Identity module's public read contract for other modules (in-process, via DI — never a
/// direct reference to IIdentityDbContext/the Users table from outside this module).</summary>
public interface IUserLookupService
{
    Task<UserLookupResult?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserLookupResult?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}

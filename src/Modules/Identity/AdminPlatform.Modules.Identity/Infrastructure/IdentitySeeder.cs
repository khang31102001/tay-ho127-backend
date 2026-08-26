using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Identity.Infrastructure;

/// <summary>Idempotent SuperAdmin account seed. Reads credentials from configuration/environment only —
/// never hardcoded (constraints.md: "Không hardcode mật khẩu"). Safe to run on every deploy: upserts by
/// email, never creates a duplicate.</summary>
public static class IdentitySeeder
{
    public const string AdminEmailConfigKey = "SEED_ADMIN_EMAIL";
    public const string AdminPasswordConfigKey = "SEED_ADMIN_PASSWORD";
    public const string AdminFullNameConfigKey = "SEED_ADMIN_FULLNAME";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IIdentityDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var email = configuration[AdminEmailConfigKey];
        var password = configuration[AdminPasswordConfigKey];
        var fullName = configuration[AdminFullNameConfigKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SEED_ADMIN_EMAIL and SEED_ADMIN_PASSWORD environment variables must be set to seed the SuperAdmin account.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return;
        }

        var admin = User.Create(email, passwordHasher.Hash(password), string.IsNullOrWhiteSpace(fullName) ? "Super Admin" : fullName);
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
    }
}

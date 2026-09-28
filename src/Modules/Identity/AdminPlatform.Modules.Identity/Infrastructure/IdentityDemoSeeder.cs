using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Identity.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only — never run against production): one admin
/// user per demo persona ("manager", "staff", "viewer"), upserted by email. The persona keys are how the
/// Migrator links these users to the demo roles/departments/brands seeded by other modules.</summary>
public static class IdentityDemoSeeder
{
    private static readonly (string Persona, string Email, string FullName)[] DemoUsers =
    [
        ("manager", "manager@demo.tayho127.test", "Demo Manager"),
        ("staff", "staff@demo.tayho127.test", "Demo Staff"),
        ("viewer", "viewer@demo.tayho127.test", "Demo Viewer"),
    ];

    /// <summary>Returns persona → user id for every demo user (existing or newly created).</summary>
    public static async Task<IReadOnlyDictionary<string, Guid>> SeedAsync(
        IServiceProvider services, string password, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IIdentityDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        var emails = DemoUsers.Select(u => u.Email).ToList();
        var existing = await db.Users
            .Where(u => emails.Contains(u.Email))
            .ToDictionaryAsync(u => u.Email, cancellationToken);

        foreach (var (_, email, fullName) in DemoUsers)
        {
            if (!existing.ContainsKey(email))
            {
                var user = User.Create(email, passwordHasher.Hash(password), fullName);
                db.Users.Add(user);
                existing[email] = user;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return DemoUsers.ToDictionary(u => u.Persona, u => existing[u.Email].Id);
    }
}

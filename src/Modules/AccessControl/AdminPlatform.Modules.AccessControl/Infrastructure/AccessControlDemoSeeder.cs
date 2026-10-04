using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.AccessControl.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only): one role per demo persona with a
/// progressively narrower permission set, assigned to that persona's user. Run after the base
/// <see cref="AccessControlSeeder"/> so the permission catalog already exists. Roles are upserted by Code
/// and only missing grants/assignments are added — nothing is ever removed.</summary>
public static class AccessControlDemoSeeder
{
    private sealed record DemoRole(string Persona, string Code, string Name, Func<string, bool> Grants);

    private static readonly DemoRole[] DemoRoles =
    [
        // Day-to-day administration: everything except managing the security model itself and deletes.
        new("manager", "demo-manager", "Demo Manager", code =>
            IsView(code)
            || (!code.StartsWith("roles.", StringComparison.Ordinal)
                && !code.StartsWith("permissions.", StringComparison.Ordinal)
                && code is not ("users.roles.manage" or "menus.permissions.manage")
                && !code.EndsWith(".delete", StringComparison.Ordinal))),

        // Read access (minus the audit trail) plus maintaining the media library.
        new("staff", "demo-staff", "Demo Staff", code =>
            (IsView(code) && code != "audit-logs.view") || code is "media.create" or "media.update"),

        new("viewer", "demo-viewer", "Demo Viewer", IsView),
    ];

    public static async Task SeedAsync(
        IServiceProvider services, IReadOnlyDictionary<string, Guid> userIdsByPersona, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAccessControlDbContext>();
        var permissions = await db.Permissions.ToListAsync(cancellationToken);

        foreach (var demoRole in DemoRoles)
        {
            var role = await db.Roles.SingleOrDefaultAsync(r => r.Code == demoRole.Code, cancellationToken);
            if (role is null)
            {
                role = Role.Create(demoRole.Code, demoRole.Name);
                db.Roles.Add(role);
                await db.SaveChangesAsync(cancellationToken);
            }

            var grantedIds = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken)).ToHashSet();

            foreach (var permission in permissions.Where(p => !p.IsGroup && demoRole.Grants(p.Code) && !grantedIds.Contains(p.Id)))
            {
                db.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id));
            }

            if (userIdsByPersona.TryGetValue(demoRole.Persona, out var userId)
                && !await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken))
            {
                db.UserRoles.Add(UserRole.Create(userId, role.Id));
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsView(string code) => code.EndsWith(".view", StringComparison.Ordinal);
}

using System.Security.Cryptography;
using AdminPlatform.Modules.AccessControl.Api;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.Modules.Identity.Api;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Infrastructure;
using AdminPlatform.Modules.Navigation.Api;
using AdminPlatform.Modules.Navigation.Infrastructure;
using AdminPlatform.Modules.Organization.Api;
using AdminPlatform.Modules.Organization.Infrastructure;
using AdminPlatform.Modules.Platform.Api;
using AdminPlatform.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AdminPlatform.IntegrationTests;

/// <summary>Spins up a real Postgres container (Testcontainers), points the whole app at it, migrates
/// every module, and seeds a SuperAdmin with the full cross-module permission catalog — mirroring what
/// AdminPlatform.Migrator does in `all` mode, but inline so tests get a ready-to-use admin account.
/// NOTE: requires Docker; see README "Known limitations" — not executable in a Docker-less sandbox.</summary>
public sealed class AdminPlatformApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("adminplatform_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string AdminEmail { get; } = "admin@integration.test";
    public string AdminPassword { get; } = "Integration-Test-Passw0rd!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                [IdentitySeeder.AdminEmailConfigKey] = AdminEmail,
                [IdentitySeeder.AdminPasswordConfigKey] = AdminPassword,
            });
        });
    }

    private static readonly string DiagPath = Path.Combine(AppContext.BaseDirectory, "diag.log");

    private static void Diag(string message)
    {
        try
        {
            File.AppendAllText(DiagPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // best-effort diagnostic only
        }
    }

    private static async Task MigrateWithDiagAsync(string label, Func<CancellationToken, Task> migrate)
    {
        Diag($"before {label} migrate");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await migrate(cts.Token);
            Diag($"after {label} migrate");
        }
        catch (Exception ex)
        {
            Diag($"EXCEPTION during {label} migrate: {ex}");
            throw;
        }
    }

    public async Task InitializeAsync()
    {
        Diag("before postgres.StartAsync");
        await _postgres.StartAsync();
        Diag("after postgres.StartAsync");
        Diag($"connection string: {_postgres.GetConnectionString()}");

        Diag("before Services.CreateScope");
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;
        Diag("after Services.CreateScope");

        await MigrateWithDiagAsync("Identity", ct => services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(ct));
        await MigrateWithDiagAsync("AccessControl", ct => services.GetRequiredService<AccessControlDbContext>().Database.MigrateAsync(ct));
        await MigrateWithDiagAsync("Organization", ct => services.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync(ct));
        await MigrateWithDiagAsync("Navigation", ct => services.GetRequiredService<NavigationDbContext>().Database.MigrateAsync(ct));
        await MigrateWithDiagAsync("Platform", ct => services.GetRequiredService<PlatformDbContext>().Database.MigrateAsync(ct));
        await MigrateWithDiagAsync("Customer", ct => services.GetRequiredService<CustomerDbContext>().Database.MigrateAsync(ct));

        Diag("before IdentitySeeder.SeedAsync");
        await IdentitySeeder.SeedAsync(services, CancellationToken.None);
        Diag("after IdentitySeeder.SeedAsync, before FindByEmailAsync");
        var admin = await services.GetRequiredService<IUserLookupService>().FindByEmailAsync(AdminEmail, CancellationToken.None);
        Diag("after FindByEmailAsync");

        IReadOnlyCollection<(string Code, string Description)> allPermissions =
        [
            .. IdentityPermissions.All,
            .. AccessControlPermissions.All,
            .. OrganizationPermissions.All,
            .. NavigationPermissions.All,
            .. PlatformPermissions.All,
        ];
        Diag("before AccessControlSeeder.SeedAsync");
        await AccessControlSeeder.SeedAsync(services, allPermissions, admin!.Id, CancellationToken.None);
        Diag("after AccessControlSeeder.SeedAsync, before NavigationSeeder.SeedAsync");
        await NavigationSeeder.SeedAsync(services, CancellationToken.None);
        Diag("after NavigationSeeder.SeedAsync — InitializeAsync complete");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

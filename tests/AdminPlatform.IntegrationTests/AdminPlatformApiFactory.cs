using System.Security.Cryptography;
using AdminPlatform.Modules.AccessControl.Api;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.Modules.Identity.Api;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Infrastructure;
using AdminPlatform.Modules.Media.Api;
using AdminPlatform.Modules.Media.Infrastructure;
using AdminPlatform.Modules.Navigation.Api;
using AdminPlatform.Modules.Navigation.Infrastructure;
using AdminPlatform.Modules.Organization.Api;
using AdminPlatform.Modules.Organization.Infrastructure;
using AdminPlatform.Modules.Platform.Api;
using AdminPlatform.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

    private readonly string _jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    /// <summary>Sets real OS environment variables (not WebApplicationFactory's ConfigureAppConfiguration)
    /// so every module's AddXModule(configuration) sees the test Postgres connection string: each module
    /// reads configuration.GetConnectionString("Default") eagerly, at registration time, before
    /// builder.Build() runs — and WebApplicationFactory's DeferredHostBuilder only merges
    /// ConfigureAppConfiguration overrides in AT Build() time, too late for those eager reads. Environment
    /// variables are picked up immediately when WebApplication.CreateBuilder(args) constructs its
    /// configuration, so this must be called before the first access to Services/CreateClient() (which
    /// triggers the host build).</summary>
    private void SetTestEnvironmentVariables(string connectionString)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", _jwtSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "AdminPlatform");
        Environment.SetEnvironmentVariable("Jwt__Audience", "AdminPlatform.Clients");
        Environment.SetEnvironmentVariable(IdentitySeeder.AdminEmailConfigKey, AdminEmail);
        Environment.SetEnvironmentVariable(IdentitySeeder.AdminPasswordConfigKey, AdminPassword);
        // The whole suite shares one client address; the production auth limit (10/min) would 429 it.
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "100000");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        SetTestEnvironmentVariables(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AccessControlDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NavigationDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<MediaDbContext>().Database.MigrateAsync();

        await IdentitySeeder.SeedAsync(services, CancellationToken.None);
        var admin = await services.GetRequiredService<IUserLookupService>().FindByEmailAsync(AdminEmail, CancellationToken.None);

        IReadOnlyCollection<(string Code, string Description)> allPermissions =
        [
            .. IdentityPermissions.All,
            .. AccessControlPermissions.All,
            .. OrganizationPermissions.All,
            .. NavigationPermissions.All,
            .. PlatformPermissions.All,
            .. MediaPermissions.All,
        ];
        await AccessControlSeeder.SeedAsync(services, allPermissions, admin!.Id, CancellationToken.None);
        await NavigationSeeder.SeedAsync(services, CancellationToken.None);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

using System.Net.Sockets;
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
using Npgsql;
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

    private static async Task RawTcpCheckAsync(string host, int port)
    {
        Diag($"before raw TCP connect to {host}:{port}");
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var winner = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(8)));
            if (winner != connectTask)
            {
                Diag($"raw TCP connect to {host}:{port} DID NOT COMPLETE within 8s (still pending)");
                return;
            }

            if (connectTask.IsFaulted)
            {
                Diag($"raw TCP connect to {host}:{port} FAILED: {connectTask.Exception}");
                return;
            }

            Diag($"raw TCP connect to {host}:{port} SUCCEEDED, Connected={tcp.Connected}");
        }
        catch (Exception ex)
        {
            Diag($"raw TCP connect to {host}:{port} EXCEPTION: {ex}");
        }
    }

    private static async Task RawNpgsqlCheckAsync(string connectionString)
    {
        Diag("before raw Npgsql connection open");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var conn = new NpgsqlConnection(connectionString);
            var openTask = conn.OpenAsync(cts.Token);
            var winner = await Task.WhenAny(openTask, Task.Delay(TimeSpan.FromSeconds(12)));
            if (winner != openTask)
            {
                Diag("raw Npgsql OpenAsync DID NOT COMPLETE within 12s (still pending)");
                return;
            }

            if (openTask.IsFaulted)
            {
                Diag($"raw Npgsql OpenAsync FAILED: {openTask.Exception}");
                return;
            }

            Diag($"raw Npgsql connection OPENED, State={conn.State}, ServerVersion={conn.PostgreSqlVersion}");

            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var result = await cmd.ExecuteScalarAsync(cts.Token);
            Diag($"raw Npgsql SELECT 1 returned: {result}");
        }
        catch (Exception ex)
        {
            Diag($"raw Npgsql EXCEPTION: {ex}");
        }
    }

    private static async Task RawEfCoreCheckAsync(string connectionString)
    {
        Diag("before manual IdentityDbContext (bypassing DI) migrate");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
                .UseSnakeCaseNamingConvention()
                .Options;
            await using var db = new IdentityDbContext(options);
            var migrateTask = db.Database.MigrateAsync(cts.Token);
            var winner = await Task.WhenAny(migrateTask, Task.Delay(TimeSpan.FromSeconds(17)));
            if (winner != migrateTask)
            {
                Diag("manual IdentityDbContext migrate DID NOT COMPLETE within 17s (still pending)");
                return;
            }

            if (migrateTask.IsFaulted)
            {
                Diag($"manual IdentityDbContext migrate FAILED: {migrateTask.Exception}");
                return;
            }

            Diag("manual IdentityDbContext migrate SUCCEEDED");
        }
        catch (Exception ex)
        {
            Diag($"manual IdentityDbContext migrate EXCEPTION: {ex}");
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
        var connString = _postgres.GetConnectionString();
        Diag($"connection string: {connString}");
        Diag($"testcontainers Hostname={_postgres.Hostname}, MappedPort={_postgres.GetMappedPublicPort(5432)}");

        await RawTcpCheckAsync(_postgres.Hostname, _postgres.GetMappedPublicPort(5432));
        await RawNpgsqlCheckAsync(connString);
        await RawEfCoreCheckAsync(connString);

        SetTestEnvironmentVariables(connString);
        Diag($"environment variables set; ConnectionStrings__Default now = {Environment.GetEnvironmentVariable("ConnectionStrings__Default")}");

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

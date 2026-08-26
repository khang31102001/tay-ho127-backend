using AdminPlatform.Common;
using AdminPlatform.Migrator;
using AdminPlatform.Modules.AccessControl;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Identity;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Identity.Infrastructure;
using AdminPlatform.Modules.Navigation;
using AdminPlatform.Modules.Navigation.Infrastructure;
using AdminPlatform.Modules.Organization;
using AdminPlatform.Modules.Organization.Infrastructure;
using AdminPlatform.Modules.Platform;
using AdminPlatform.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var appBuilder = Host.CreateApplicationBuilder(args);

// Each module's AddXModule() also registers ASP.NET Core MVC (AddControllers) so the same registration
// works for the Host. That drags in endpoint-routing services that only resolve inside a real web host —
// harmless since this tool never touches a controller, but Host.CreateApplicationBuilder's default
// Development-time DI validation would otherwise fail eagerly on them. Validation is disabled here only;
// the Host (Program.cs) keeps full validation.
appBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = false,
    ValidateScopes = false,
}));

appBuilder.Services.AddPlatformCommon();

// Migrator never issues tokens or switches working context — these are inert stand-ins so the DI
// container still builds; the Host's real adapters are what the running API uses.
appBuilder.Services.AddScoped<IUserPermissionsProvider, NullUserPermissionsProvider>();
appBuilder.Services.AddScoped<IUserScopeValidator, NullUserScopeValidator>();

appBuilder.Services.AddIdentityModule(appBuilder.Configuration);
appBuilder.Services.AddAccessControlModule(appBuilder.Configuration);
appBuilder.Services.AddOrganizationModule(appBuilder.Configuration);
appBuilder.Services.AddNavigationModule(appBuilder.Configuration);
appBuilder.Services.AddPlatformModule(appBuilder.Configuration);

using var host = appBuilder.Build();
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Migrator");

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

try
{
    if (command is "migrate" or "all")
    {
        await MigrateAsync(services, logger);
    }

    if (command is "seed" or "all")
    {
        await SeedAsync(services, logger);
    }

    if (command is not ("migrate" or "seed" or "all"))
    {
        logger.LogError("Unknown command '{Command}'. Expected: migrate | seed | all", command);
        return 1;
    }
}
catch (Exception ex)
{
    // Non-zero exit stops the deployment pipeline/init container — never a silent failure
    // (constraints.md: "phải chạy bằng deployment job riêng, có log và dừng triển khai nếu thất bại").
    logger.LogError(ex, "Migrator command '{Command}' failed", command);
    return 1;
}

return 0;

static async Task MigrateAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Applying Identity module migrations...");
    await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying AccessControl module migrations...");
    await services.GetRequiredService<AccessControlDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Organization module migrations...");
    await services.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Navigation module migrations...");
    await services.GetRequiredService<NavigationDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Platform module migrations...");
    await services.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();

    logger.LogInformation("All migrations applied.");
}

static async Task SeedAsync(IServiceProvider services, ILogger logger)
{
    var cancellationToken = CancellationToken.None;
    var configuration = services.GetRequiredService<IConfiguration>();

    logger.LogInformation("Seeding Identity module (SuperAdmin account)...");
    await IdentitySeeder.SeedAsync(services, cancellationToken);

    Guid? adminUserId = null;
    var adminEmail = configuration[IdentitySeeder.AdminEmailConfigKey];
    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var admin = await services.GetRequiredService<IUserLookupService>().FindByEmailAsync(adminEmail, cancellationToken);
        adminUserId = admin?.Id;
    }

    logger.LogInformation("Seeding AccessControl module (permission catalog, SuperAdmin role)...");
    await AccessControlSeeder.SeedAsync(services, PermissionCatalog.All, adminUserId, cancellationToken);

    logger.LogInformation("Seeding Organization module (sample org/department/brand)...");
    var sampleOrganizationId = await OrganizationSeeder.SeedAsync(services, cancellationToken);

    logger.LogInformation("Seeding Navigation module (base menu tree)...");
    await NavigationSeeder.SeedAsync(services, cancellationToken);

    logger.LogInformation("Seeding Platform module (sample fiscal year)...");
    await PlatformSeeder.SeedAsync(services, sampleOrganizationId, cancellationToken);

    logger.LogInformation("Seed complete.");
}

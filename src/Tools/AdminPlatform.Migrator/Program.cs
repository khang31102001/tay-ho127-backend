using AdminPlatform.Common;
using AdminPlatform.Migrator;
using AdminPlatform.Modules.AccessControl;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Catalog;
using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Infrastructure;
using AdminPlatform.Modules.Content;
using AdminPlatform.Modules.Content.Infrastructure;
using AdminPlatform.Modules.Sales;
using AdminPlatform.Modules.Sales.Infrastructure;
using AdminPlatform.Modules.Seo;
using AdminPlatform.Modules.Seo.Infrastructure;
using AdminPlatform.Modules.Customer;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.Modules.Identity;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Identity.Infrastructure;
using AdminPlatform.Modules.Media;
using AdminPlatform.Modules.Media.Infrastructure;
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
appBuilder.Services.AddCustomerModule(appBuilder.Configuration);
appBuilder.Services.AddMediaModule(appBuilder.Configuration);
appBuilder.Services.AddCatalogModule(appBuilder.Configuration);
appBuilder.Services.AddContentModule(appBuilder.Configuration);
appBuilder.Services.AddSeoModule(appBuilder.Configuration);
appBuilder.Services.AddSalesModule(appBuilder.Configuration);

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

    // Demo data is never part of "all": it must be asked for explicitly, and only for test/dev databases.
    if (command is "seed-demo")
    {
        await SeedDemoAsync(services, logger);
    }

    // Seeds only the Seo module — for filling SEO defaults on an already-provisioned database without
    // touching other modules' data.
    if (command is "seed-seo")
    {
        logger.LogInformation("Seeding Seo module (default SEO settings, only when absent)...");
        var seoOnlySeeded = await SeoSeeder.SeedAsync(services, CancellationToken.None);
        logger.LogInformation(seoOnlySeeded ? "SEO settings seeded." : "SEO settings already exist - left untouched.");
    }

    // Seeds only the Sales module — for filling the order configuration on an already-provisioned database.
    if (command is "seed-sales")
    {
        logger.LogInformation("Seeding Sales module (only into empty tables)...");
        var salesOnlySeeded = await SalesSeeder.SeedAsync(services, CancellationToken.None);
        logger.LogInformation(salesOnlySeeded ? "Sales configuration seeded." : "Sales configuration already exists - left untouched.");
    }

    if (command is not ("migrate" or "seed" or "all" or "seed-demo" or "seed-seo" or "seed-sales"))
    {
        logger.LogError("Unknown command '{Command}'. Expected: migrate | seed | all | seed-demo | seed-seo | seed-sales", command);
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

    logger.LogInformation("Applying Customer module migrations...");
    await services.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Media module migrations...");
    await services.GetRequiredService<MediaDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Catalog module migrations...");
    await services.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Content module migrations...");
    await services.GetRequiredService<ContentDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Seo module migrations...");
    await services.GetRequiredService<SeoDbContext>().Database.MigrateAsync();

    logger.LogInformation("Applying Sales module migrations...");
    await services.GetRequiredService<SalesDbContext>().Database.MigrateAsync();

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

    logger.LogInformation("Seeding Catalog module (initial restaurant menu, only into an empty catalog)...");
    var catalogSeeded = await CatalogSeeder.SeedAsync(services, cancellationToken);
    logger.LogInformation(catalogSeeded ? "Catalog seeded." : "Catalog already has data - left untouched.");

    logger.LogInformation("Seeding Content module (initial articles, categories and tags, only into empty tables)...");
    var contentSeeded = await ContentSeeder.SeedAsync(services, cancellationToken);
    logger.LogInformation(contentSeeded ? "Content seeded." : "Content already has data - left untouched.");

    logger.LogInformation("Seeding Seo module (default SEO settings, only when absent)...");
    var seoSeeded = await SeoSeeder.SeedAsync(services, cancellationToken);
    logger.LogInformation(seoSeeded ? "SEO settings seeded." : "SEO settings already exist - left untouched.");

    logger.LogInformation("Seeding Sales module (delivery/payment methods, order options, order settings, only into empty tables)...");
    var salesSeeded = await SalesSeeder.SeedAsync(services, cancellationToken);
    logger.LogInformation(salesSeeded ? "Sales configuration seeded." : "Sales configuration already exists - left untouched.");

    logger.LogInformation("Seed complete.");
}

// DEMO data for test/dev databases — run after "all" (it relies on the base permission catalog, sample
// organization and root department). Idempotent. The demo users and customers share one password, read
// from SEED_DEMO_PASSWORD so it is never hardcoded.
static async Task SeedDemoAsync(IServiceProvider services, ILogger logger)
{
    const string DemoPasswordConfigKey = "SEED_DEMO_PASSWORD";

    var cancellationToken = CancellationToken.None;
    var demoPassword = services.GetRequiredService<IConfiguration>()[DemoPasswordConfigKey];
    if (string.IsNullOrWhiteSpace(demoPassword))
    {
        throw new InvalidOperationException($"{DemoPasswordConfigKey} must be set to seed demo data.");
    }

    logger.LogInformation("Seeding demo admin users (manager, staff, viewer)...");
    var userIdsByPersona = await IdentityDemoSeeder.SeedAsync(services, demoPassword, cancellationToken);

    logger.LogInformation("Seeding demo roles and role assignments...");
    await AccessControlDemoSeeder.SeedAsync(services, userIdsByPersona, cancellationToken);

    logger.LogInformation("Seeding demo departments, brands and user scopes...");
    var sampleOrganizationId = await OrganizationSeeder.SeedAsync(services, cancellationToken);
    await OrganizationDemoSeeder.SeedAsync(services, sampleOrganizationId, userIdsByPersona, cancellationToken);

    logger.LogInformation("Seeding demo fiscal year...");
    await PlatformDemoSeeder.SeedAsync(services, sampleOrganizationId, cancellationToken);

    logger.LogInformation("Seeding demo customers and addresses...");
    await CustomerDemoSeeder.SeedAsync(services, demoPassword, cancellationToken);

    logger.LogInformation("Seeding demo media...");
    await MediaDemoSeeder.SeedAsync(services, cancellationToken);

    logger.LogInformation("Seeding demo promotions (discount codes)...");
    var promotionsCreated = await PromotionDemoSeeder.SeedAsync(services, cancellationToken);
    logger.LogInformation("{Count} demo promotion(s) created.", promotionsCreated);

    logger.LogInformation("Seeding demo sales (orders, payments and payment sessions in every status)...");
    var demoProducts = await services.GetRequiredService<ICatalogDbContext>().Products.AsNoTracking()
        .Where(p => p.IsActive)
        .Include(p => p.Media)
        .OrderBy(p => p.Name)
        .Take(4)
        .ToListAsync(cancellationToken);
    var demoOrders = await SalesDemoSeeder.SeedAsync(
        services,
        demoProducts.Select(p => new DemoProduct(p.Id, p.Name, p.Price, p.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId).FirstOrDefault())).ToList(),
        cancellationToken);
    logger.LogInformation("{Count} demo order(s) created.", demoOrders);

    logger.LogInformation("Demo seed complete.");
}

using System.Text;
using System.Threading.RateLimiting;
using AdminPlatform.Api.CrossModuleAdapters;
using AdminPlatform.Common;
using AdminPlatform.Common.Security;
using AdminPlatform.Common.Web;
using AdminPlatform.Modules.AccessControl;
using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Catalog;
using AdminPlatform.Modules.Catalog.Api;
using AdminPlatform.Modules.Catalog.Infrastructure;
using AdminPlatform.Modules.Content;
using AdminPlatform.Modules.Content.Infrastructure;
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
using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Organization.Infrastructure;
using AdminPlatform.Modules.Platform;
using AdminPlatform.Modules.Platform.Application;
using AdminPlatform.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName());

    // ---- Cross-cutting building blocks (shared by every module) ----
    builder.Services.AddPlatformCommon();

    // ---- Cross-module ports, implemented at the composition root only (architecture assumption #6) ----
    builder.Services.AddScoped<IUserPermissionsProvider, IdentityPermissionsAdapter>();
    builder.Services.AddScoped<IUserScopeValidator, IdentityUserScopeAdapter>();

    // ---- Modules ----
    builder.Services.AddIdentityModule(builder.Configuration);
    builder.Services.AddAccessControlModule(builder.Configuration);
    builder.Services.AddOrganizationModule(builder.Configuration);
    builder.Services.AddNavigationModule(builder.Configuration);
    builder.Services.AddPlatformModule(builder.Configuration);
    builder.Services.AddCustomerModule(builder.Configuration);
    builder.Services.AddMediaModule(builder.Configuration);
    builder.Services.AddCatalogModule(builder.Configuration);
    builder.Services.AddContentModule(builder.Configuration);

    // ---- MVC / validation ----
    builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>());
    builder.Services.AddEndpointsApiExplorer();

    // ---- Problem Details + centralized exception handling (api-design.md §18-21) ----
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ---- AuthN: JWT bearer ----
    // jwtOptions is read from IConfiguration lazily, inside Configure<IConfiguration>, rather than eagerly
    // here from builder.Configuration: WebApplicationFactory's DeferredHostBuilder (used by the integration
    // tests) only merges its test configuration overrides (e.g. a throwaway Jwt:SigningKey) at the moment
    // builder.Build() actually runs — reading builder.Configuration any earlier than that always sees the
    // real appsettings.json (SigningKey: ""), which would incorrectly throw under test.
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IConfiguration>((options, configuration) =>
        {
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException("Missing Jwt configuration section.");
            if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
            {
                throw new InvalidOperationException(
                    "Jwt:SigningKey is not set. Provide it via an environment variable or user-secrets — never commit it.");
            }

            // Keep claim names exactly as issued ("sub", "email", "role", ...). The default inbound mapping
            // renames them to long ClaimTypes URIs, so AppClaimTypes lookups (GetUserId, ICurrentUser) found
            // nothing — every /me endpoint returned 500 and audit CreatedBy/UpdatedBy were always null.
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    // ---- AuthZ: dynamic Permission:* policies (PermissionPolicyProvider registered by AddPlatformCommon),
    // plus the two fixed AccountType:* policies that separate the Admin and Customer JWT "realms" even
    // though both share this one JWT bearer scheme (api-design.md §36-37) ----
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AccountTypePolicy.NameFor(AccountTypes.Admin), policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new AccountTypeRequirement(AccountTypes.Admin)));
        options.AddPolicy(AccountTypePolicy.NameFor(AccountTypes.Customer), policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new AccountTypeRequirement(AccountTypes.Customer)));
    });

    // ---- Rate limiting for abuse-sensitive auth endpoints (api-design.md §53) ----
    // Limits come from RateLimiting:Auth (defaults: 10 requests / 60s) and are read lazily for the same
    // WebApplicationFactory reason as JwtBearerOptions above — integration tests raise the limit, since
    // every test logs in/registers through these endpoints from the same client address.
    builder.Services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
    builder.Services.AddOptions<RateLimiterOptions>()
        .Configure<IConfiguration>((options, configuration) =>
        {
            var authLimits = configuration.GetSection("RateLimiting:Auth");
            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = authLimits.GetValue("PermitLimit", 10);
                limiterOptions.Window = TimeSpan.FromSeconds(authLimits.GetValue("WindowSeconds", 60));
                limiterOptions.QueueLimit = 0;
            });

            // Anonymous checkout code validation: a generous server-wide cap (the website's server makes
            // these calls, so every shopper shares one client address) that stops code-guessing floods.
            var validateLimits = configuration.GetSection("RateLimiting:PromotionValidate");
            options.AddFixedWindowLimiter(PromotionsController.ValidateRateLimitPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = validateLimits.GetValue("PermitLimit", 120);
                limiterOptions.Window = TimeSpan.FromSeconds(validateLimits.GetValue("WindowSeconds", 60));
                limiterOptions.QueueLimit = 0;
            });
        });

    // ---- Health checks ----
    // The connection string is resolved lazily via this factory (invoked only when /health is actually
    // hit, reading IConfiguration from DI at that point) rather than read eagerly from builder.Configuration
    // here: this is the actual root cause of "entry point exited without ever building an IHost" under
    // WebApplicationFactory (integration tests) — builder.Configuration at this point in Program.cs is
    // still the real appsettings.json (ConnectionStrings:Default: ""), since WebApplicationFactory's
    // DeferredHostBuilder only merges its test config overrides at Build() time. Reading "" eagerly here
    // used to make AddNpgSql's own internal null/empty guard throw before Build() ever ran.
    builder.Services.AddHealthChecks().AddNpgSql(
        sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default."),
        name: "postgres");

    // ---- OpenAPI / Swagger with a JWT bearer scheme ----
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "AdminPlatform API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste only the access token — no \"Bearer \" prefix needed.",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            },
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        // Convenience only, per constraints.md: "Development có thể tự áp dụng migration sau khi kiểm tra
        // cấu hình" — gated by an explicit flag, never implicit. Production must use the Migrator tool.
        if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            await MigrateDevelopmentDatabaseAsync(app.Services);
        }
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "AdminPlatform.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task MigrateDevelopmentDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var provider = scope.ServiceProvider;

    await provider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<AccessControlDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<NavigationDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<ContentDbContext>().Database.MigrateAsync();
}

public partial class Program;

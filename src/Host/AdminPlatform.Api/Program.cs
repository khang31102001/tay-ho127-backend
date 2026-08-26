using System.Text;
using System.Threading.RateLimiting;
using AdminPlatform.Api.CrossModuleAdapters;
using AdminPlatform.Common;
using AdminPlatform.Common.Security;
using AdminPlatform.Common.Web;
using AdminPlatform.Modules.AccessControl;
using AdminPlatform.Modules.AccessControl.Application;
using AdminPlatform.Modules.AccessControl.Infrastructure;
using AdminPlatform.Modules.Identity;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Identity.Infrastructure;
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

    // ---- MVC / validation ----
    builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>());
    builder.Services.AddEndpointsApiExplorer();

    // ---- Problem Details + centralized exception handling (api-design.md §18-21) ----
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ---- AuthN: JWT bearer ----
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Missing Jwt configuration section.");
    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey is not set. Provide it via an environment variable or user-secrets — never commit it.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
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

    // ---- AuthZ: dynamic Permission:* policies (PermissionPolicyProvider registered by AddPlatformCommon) ----
    builder.Services.AddAuthorization();

    // ---- Rate limiting for abuse-sensitive auth endpoints (api-design.md §53) ----
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });
    });

    // ---- Health checks ----
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");
    builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgres");

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

    if (app.Environment.IsDevelopment())
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
}

public partial class Program;

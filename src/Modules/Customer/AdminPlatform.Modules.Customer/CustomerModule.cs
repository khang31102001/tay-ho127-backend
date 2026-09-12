using AdminPlatform.Common.Persistence;
using AdminPlatform.Common.Security;
using FluentValidation;
using AdminPlatform.Modules.Customer.Application;
using AdminPlatform.Modules.Customer.Application.Addresses;
using AdminPlatform.Modules.Customer.Application.Auth;
using AdminPlatform.Modules.Customer.Application.Customers;
using AdminPlatform.Modules.Customer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Customer;

/// <summary>Composition entry point for the Customer module. The Host calls AddCustomerModule() once;
/// everything the module needs (DbContext, services, validators, controllers) is registered here — see
/// AdminPlatform.Modules.Identity.IdentityModule for the pattern this mirrors.</summary>
public static class CustomerModule
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<CustomerDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CustomerDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<ICustomerDbContext>(sp => sp.GetRequiredService<CustomerDbContext>());

        // Same Jwt:* configuration section Identity binds — Customer tokens are issued by the shared
        // Common.Security.IJwtTokenService using the exact same issuer/audience/signing key (§6: "Reuse Auth
        // infrastructure hiện tại"), distinguished only by the account_type claim (§7).
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));

        services.AddScoped<IGoogleIdTokenVerifier, GoogleIdTokenVerifier>();
        services.AddScoped<ICustomerAuthService, CustomerAuthService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();

        services.AddValidatorsFromAssembly(typeof(CustomerModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(CustomerModule).Assembly);

        return services;
    }
}

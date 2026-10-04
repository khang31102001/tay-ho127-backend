using AdminPlatform.Common.Persistence;
using AdminPlatform.Modules.Sales.Application;
using AdminPlatform.Modules.Sales.Application.DeliveryMethods;
using AdminPlatform.Modules.Sales.Application.OrderOptions;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.PaymentMethods;
using AdminPlatform.Modules.Sales.Application.Payments;
using AdminPlatform.Modules.Sales.Application.PaymentSessions;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Application.Settings;
using AdminPlatform.Modules.Sales.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminPlatform.Modules.Sales;

/// <summary>Composition entry point for the Sales module: delivery and payment methods, general order options, order
/// settings, orders, payments and payment sessions. The Host calls AddSalesModule() once and ALSO registers the
/// ports Sales needs (ICatalogPricingProvider, IPromotionPricing, ICustomerDirectory) — see CrossModuleAdapters.</summary>
public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<SalesDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", SalesDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<ISalesDbContext>(sp => sp.GetRequiredService<SalesDbContext>());

        services.AddScoped<IDeliveryMethodService, DeliveryMethodService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<IOrderOptionService, OrderOptionService>();
        services.AddScoped<IOrderSettingsService, OrderSettingsService>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<OrderPricer>();
        services.AddScoped<OrderPlacer>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentSessionService, PaymentSessionService>();

        services.AddScoped<IOrderCodeGenerator, OrderCodeGenerator>();
        // The default notifier only logs; a host that wires a real email/SMS notifier registers it first and this is skipped.
        services.TryAddScoped<IOrderNotifier, LoggingOrderNotifier>();

        services.AddValidatorsFromAssembly(typeof(SalesModule).Assembly);

        services.AddControllers().AddApplicationPart(typeof(SalesModule).Assembly);

        return services;
    }
}

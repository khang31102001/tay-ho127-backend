using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Common.Messaging;

public static class EsmsServiceCollectionExtensions
{
    /// <summary>Binds the "Esms" section and registers <see cref="IEsmsClient"/>. Call once from the Host before the
    /// modules that send messages (Customer, Sales). Safe to run without keys: callers check
    /// <see cref="EsmsOptions.IsConfigured"/> to decide between sending and logging.</summary>
    public static IServiceCollection AddEsmsMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EsmsOptions>(configuration.GetSection(EsmsOptions.SectionName));
        services.AddHttpClient<IEsmsClient, EsmsClient>(client => client.Timeout = TimeSpan.FromSeconds(15));
        return services;
    }
}

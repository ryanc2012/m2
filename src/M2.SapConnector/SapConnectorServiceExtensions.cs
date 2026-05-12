using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace M2.SapConnector;

public static class SapConnectorServiceExtensions
{
    public static IServiceCollection AddSapConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SapConnectorOptions>(
            configuration.GetSection(SapConnectorOptions.SectionName));

        // Typed HttpClient for OData — base address resolved from IConfiguration in SapODataClient constructor
        services.AddHttpClient<ISapODataClient, SapODataClient>();

        // NCo stub — throws NotSupportedException if any RFC method is invoked
        services.AddSingleton<ISapNcoClient, SapNcoClient>();

        return services;
    }
}

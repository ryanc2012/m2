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

        services.AddSingleton<ISapODataClient, NoOpSapODataClient>();
        services.AddSingleton<ISapNcoClient, NoOpSapNcoClient>();

        return services;
    }
}

using M2.Infrastructure.InterModule.Http;
using M2.Infrastructure.InterModule.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M2.Infrastructure.InterModule;

public static class InterModuleServiceExtensions
{
    public static IServiceCollection AddInterModuleClients(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHttpClient<INotificationsModuleClient, NotificationsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(config["Platform:BaseUrl"] ?? "https://localhost:5000");
        });
        // future modules added here
        return services;
    }
}

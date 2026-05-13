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
        var baseUrl = new Uri(config["Platform:BaseUrl"] ?? "https://localhost:5100");
        var apiKey = config["Platform:ApiKey"] ?? "platform-dev-key";

        void ConfigureClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        services.AddHttpClient<INotificationsModuleClient, NotificationsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/notifications/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IMembersModuleClient, MembersModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/members/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IApprovalsModuleClient, ApprovalsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/approvals/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IPromotionsModuleClient, PromotionsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/promotions/");
            ConfigureClient(client);
        });

        services.AddHttpClient<ISalesModuleClient, SalesModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/sales/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IAttendanceModuleClient, AttendanceModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/attendance/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IGoodsReceiptModuleClient, GoodsReceiptModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/goods-receipt/");
            ConfigureClient(client);
        });

        services.AddHttpClient<IReportingModuleClient, ReportingModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/reporting/");
            ConfigureClient(client);
        });

        return services;
    }
}

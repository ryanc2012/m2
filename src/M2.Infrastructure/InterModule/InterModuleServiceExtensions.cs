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
        var baseUrl = new Uri(config["Platform:BaseUrl"] ?? "https://localhost:5000");

        services.AddHttpClient<INotificationsModuleClient, NotificationsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/notifications/");
        });

        services.AddHttpClient<IMembersModuleClient, MembersModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/members/");
        });

        services.AddHttpClient<IApprovalsModuleClient, ApprovalsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/approvals/");
        });

        services.AddHttpClient<IPromotionsModuleClient, PromotionsModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/promotions/");
        });

        services.AddHttpClient<ISalesModuleClient, SalesModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/sales/");
        });

        services.AddHttpClient<IAttendanceModuleClient, AttendanceModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/attendance/");
        });

        services.AddHttpClient<IGoodsReceiptModuleClient, GoodsReceiptModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/goods-receipt/");
        });

        services.AddHttpClient<IReportingModuleClient, ReportingModuleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, "/modules/reporting/");
        });

        return services;
    }
}

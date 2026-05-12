using M2.Domain.Approvals;
using M2.Domain.Attendance;
using M2.Domain.Members;
using M2.Domain.Notifications;
using M2.Domain.Promotions;
using M2.Domain.Sales;
using M2.Infrastructure.Approvals;
using M2.Infrastructure.Attendance;
using M2.Infrastructure.Members;
using M2.Infrastructure.Notifications;
using M2.Infrastructure.Promotions;
using M2.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M2.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<M2DbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(M2DbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "m2");
                }));

        services.AddScoped<IOutboxService, NoOpOutboxService>();

        // Approvals
        services.AddScoped<IApprovalPolicyService, ApprovalPolicyService>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // Notifications
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<ISmsGateway, NoOpSmsGateway>();

        // Members
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IOtpService, OtpService>();

        // Promotions
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IDiscountEngine, DiscountEngine>();

        // Sales
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IReturnService, ReturnService>();

        // Attendance
        services.AddScoped<IAttendanceService, AttendanceService>();

        return services;
    }
}

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using M2.Domain.Approvals;
using M2.Domain.Attendance;
using M2.Domain.GoodsReceipt;
using M2.Domain.Members;
using M2.Domain.Notifications;
using M2.Domain.Promotions;
using M2.Domain.Reporting;
using M2.Domain.Sales;
using M2.Infrastructure.Approvals;
using M2.Infrastructure.Attendance;
using M2.Infrastructure.GoodsReceipt;
using M2.Infrastructure.Members;
using M2.Infrastructure.Notifications;
using M2.Infrastructure.Outbox;
using M2.Infrastructure.Promotions;
using M2.Infrastructure.Reporting;
using M2.Infrastructure.Sales;
using M2.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace M2.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddDbContext<M2DbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(M2DbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "m2");
                }));

        // Hangfire — PostgreSQL-backed job storage (ADR-006/ADR-017)
        // Only registered when called with an IHostEnvironment (i.e., from Platform.Api).
        // BFFs call AddInfrastructure without environment and skip the worker registration.
        if (environment != null)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"))));
            services.AddHangfireServer();
        }

        // MediatR — domain event publishing
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceExtensions).Assembly));

        // Firebase Admin SDK — initialise once using ADC (GOOGLE_APPLICATION_CREDENTIALS).
        // Skipped gracefully if credentials are not configured (dev/test with no Firebase project).
        if (FirebaseApp.DefaultInstance is null)
        {
            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.GetApplicationDefault()
                });
            }
            catch (Exception ex) when (environment?.IsDevelopment() == true || environment is null)
            {
                // ADC not configured — log and continue; FCM calls will be skipped at runtime
                var _ = ex; // suppress unused-variable warning; caller observes null DefaultInstance
            }
        }

        // Outbox (real EF-backed, replaces NoOp)
        services.AddScoped<IOutboxService, OutboxService>();

        // Approvals
        services.AddScoped<IApprovalPolicyService, ApprovalPolicyService>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // Notifications
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationHistoryService, NotificationHistoryService>();
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
        services.AddScoped<IIdempotencyContext, IdempotencyContext>();

        // Attendance
        services.AddScoped<IAttendanceService, AttendanceService>();

        // Goods Receipt
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();

        // Reporting
        services.AddScoped<IReportingService, ReportingService>();

        // Dev seed — only in Development environment (Edie's DevSeedService)
        if (environment?.IsDevelopment() == true)
        {
            services.AddHostedService<DevSeedService>();
        }

        return services;
    }
}


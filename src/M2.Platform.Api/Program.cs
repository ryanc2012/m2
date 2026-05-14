using Hangfire;
using M2.Infrastructure;
using M2.Infrastructure.Modules;
using M2.Infrastructure.Notifications;
using M2.Infrastructure.Outbox;
using M2.Platform.Api.Hubs;
using M2.SapConnector;
using M2.SharedKernel;
using M2.SharedKernel.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Encodings.Web;

namespace M2.Platform.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((ctx, services, config) => config
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console());

            // All domain services and EF Core
            builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

            // SAP OData / NCo clients
            builder.Services.AddSapConnector(builder.Configuration);

            // Inter-module auth options (Platform:InternalCallSecret for intra-platform module calls)
            builder.Services.AddInterModuleAuth(builder.Configuration);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
                c.SwaggerDoc("v1", new() { Title = "M2 Platform API", Version = "v1" }));

            // Registers the default "ApiKey" auth scheme.  The real API key / internal-call
            // validation is handled by ApiKeyMiddleware which runs immediately after
            // UseAuthentication() and sets context.User before UseAuthorization() fires.
            // In integration tests, PlatformWebApplicationFactory replaces this scheme with
            // "Test" (TestAuthHandler) so all test requests are pre-authenticated.
            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<AuthenticationSchemeOptions, PlatformNoOpAuthHandler>("ApiKey", _ => { });
            builder.Services.AddAuthorization();

            builder.Services.AddHealthChecks()
                .AddNpgSql(
                    builder.Configuration.GetConnectionString("DefaultConnection")!,
                    name: "postgres",
                    tags: ["ready"]);
            builder.Services.AddProblemDetails();

            // SignalR — in-process push to Portal/BFF connected clients
            builder.Services.AddSignalR();
            builder.Services.AddScoped<ISignalRNotificationDispatcher, SignalRNotificationDispatcher>();

            var app = builder.Build();

            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();

            app.UseSwagger();
            app.UseSwaggerUI();

            // X-Api-Key WebSocket auth: promote ?access_token= to Authorization header for SignalR
            app.Use(async (context, next) =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Request.Headers["X-Api-Key"] = accessToken.ToString();
                await next(context);
            });

            app.UseAuthentication();
            // Validates X-Api-Key from BFFs; grants InternalCall identity for X-Internal-Call requests
            app.UseMiddleware<ApiKeyMiddleware>();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // SignalR hub endpoint
            app.MapHub<NotificationHub>("/hubs/notifications");

            // Hangfire dashboard — development only
            if (app.Environment.IsDevelopment())
            {
                app.MapHangfireDashboard("/hangfire");
            }

            // Register SAP outbox recurring job — every 30 seconds
            RecurringJob.AddOrUpdate<SapOutboxWorker>(
                "sap-outbox-worker",
                w => w.ProcessAsync(CancellationToken.None),
                "*/30 * * * * *");

            // All 8 domain module endpoint groups
            app.MapMembersModule();
            app.MapApprovalsModule();
            app.MapPromotionsModule();
            app.MapSalesModule();
            app.MapAttendanceModule();
            app.MapGoodsReceiptModule();
            app.MapReportingModule();
            app.MapNotificationsModule();
            app.MapApiKeyModule();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "M2.Platform.Api failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// No-op authentication handler for the platform API.
/// Real authentication is performed by <see cref="ApiKeyMiddleware"/>, which
/// sets context.User for valid internal-call requests before UseAuthorization() runs.
/// In integration tests this scheme is replaced by TestAuthHandler via
/// PlatformWebApplicationFactory.ConfigureTestServices.
/// </summary>
internal sealed class PlatformNoOpAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}


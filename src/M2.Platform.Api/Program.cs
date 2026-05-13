using M2.Infrastructure;
using M2.Infrastructure.Modules;
using M2.SharedKernel;
using M2.SharedKernel.Middleware;
using Serilog;

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
            builder.Services.AddInfrastructure(builder.Configuration);

            // Inter-module auth options (Platform:InternalCallSecret for intra-platform module calls)
            builder.Services.AddInterModuleAuth(builder.Configuration);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
                c.SwaggerDoc("v1", new() { Title = "M2 Platform API", Version = "v1" }));

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            app.UseSwagger();
            app.UseSwaggerUI();

            // Validates X-Api-Key from BFFs; grants InternalCall identity for X-Internal-Call requests
            app.UseMiddleware<ApiKeyMiddleware>();

            app.MapHealthChecks("/health");

            // All 8 domain module endpoint groups
            app.MapMembersModule();
            app.MapApprovalsModule();
            app.MapPromotionsModule();
            app.MapSalesModule();
            app.MapAttendanceModule();
            app.MapGoodsReceiptModule();
            app.MapReportingModule();
            app.MapNotificationsModule();

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

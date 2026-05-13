using M2.Infrastructure;
using M2.Infrastructure.Modules;
using M2.SharedKernel;
using M2.SharedKernel.Middleware;
using Microsoft.AspNetCore.Authentication;
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
            builder.Services.AddInfrastructure(builder.Configuration);

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

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthentication();
            // Validates X-Api-Key from BFFs; grants InternalCall identity for X-Internal-Call requests
            app.UseMiddleware<ApiKeyMiddleware>();
            app.UseAuthorization();

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

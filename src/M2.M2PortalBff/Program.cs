using M2.Infrastructure;
using M2.Infrastructure.InterModule;
using M2.M2PortalBff.Endpoints;
using M2.SapConnector;
using M2.SharedKernel;
using M2.SharedKernel.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;

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

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddInterModuleClients(builder.Configuration);
    builder.Services.AddInterModuleAuth(builder.Configuration);
    builder.Services.AddSapConnector(builder.Configuration);

    builder.Services.AddHealthChecks();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
            c.SwaggerDoc("v1", new() { Title = "M2 Portal BFF", Version = "v1" }));
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");
    app.UseMiddleware<ApiKeyMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapApprovalEndpoints();
    app.MapNotificationEndpoints();
    app.MapMemberAdminEndpoints();
    app.MapPromotionEndpoints();
    app.MapAttendanceAdminEndpoints();
    app.MapGoodsReceiptEndpoints();
    app.MapReportingEndpoints();
    app.MapNotificationHistoryAdminEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "M2PortalBff failed to start");
}
finally
{
    Log.CloseAndFlush();
}

// Required by WebApplicationFactory<Program> in integration tests (Microsoft.AspNetCore.Mvc.Testing)
public partial class Program { }

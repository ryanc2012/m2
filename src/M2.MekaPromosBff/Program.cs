using M2.Infrastructure;
using M2.Infrastructure.InterModule;
using M2.MekaPromosBff.Endpoints;
using M2.SapConnector;
using M2.SharedKernel;
using M2.SharedKernel.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

    var platformBaseUrl = builder.Configuration["Platform:BaseUrl"] ?? "https://localhost:5100";
    builder.Services.AddHealthChecks()
        .AddUrlGroup(new Uri($"{platformBaseUrl}/health/ready"), name: "platform-api", tags: ["ready"]);
    builder.Services.AddProblemDetails();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
            c.SwaggerDoc("v1", new() { Title = "MekaPromos BFF", Version = "v1" }));
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

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
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    var v1 = app.MapGroup("/api/v1");
    v1.MapMemberEndpoints();
    v1.MapCouponEndpoints();
    v1.MapNotificationHistoryEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MekaPromosBff failed to start");
}
finally
{
    Log.CloseAndFlush();
}

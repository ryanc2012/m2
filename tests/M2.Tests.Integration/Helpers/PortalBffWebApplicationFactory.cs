using M2.Domain.Approvals.Dtos;
using M2.Infrastructure;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace M2.Tests.Integration.Helpers;

/// <summary>
/// Authenticated test factory for M2PortalBff.
/// TestAuthHandler replaces Entra ID JWT — all requests are authenticated as TestUserId.
/// Module clients are mocked so tests control inter-module call results.
/// Use this factory for happy-path (2xx) and 403 tests.
/// </summary>
public class PortalBffWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IApprovalsModuleClient> ApprovalsMock { get; } = new();
    public Mock<IPromotionsModuleClient> PromotionsMock { get; } = new();
    public Mock<IGoodsReceiptModuleClient> GoodsReceiptMock { get; } = new();
    public Mock<IReportingModuleClient> ReportingMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:Audience"] = "api://test",
                ["Platform:BaseUrl"] = "https://platform-test.invalid",
                ["Platform:ApiKey"] = "test-api-key",
                ["Platform:InternalCallSecret"] = "internal",
                ["SapConnector:BaseUrl"] = "https://sap-test.invalid",
                ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL DbContext with in-memory.
            // UseInternalServiceProvider avoids the "two database providers" collision between
            // Npgsql (registered by AddInfrastructure) and InMemory added here.
            services.RemoveAll<DbContextOptions<M2DbContext>>();
            var inMemorySp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<M2DbContext>(opts =>
                opts.UseInMemoryDatabase($"PortalBffTestDb-{Guid.NewGuid()}")
                    .UseInternalServiceProvider(inMemorySp));

            // Replace Entra ID JWT with test scheme — all requests authenticated as TestUserId.
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Replace module HTTP clients with controllable mocks.
            services.RemoveAll<IApprovalsModuleClient>();
            services.AddScoped<IApprovalsModuleClient>(_ => ApprovalsMock.Object);

            services.RemoveAll<IPromotionsModuleClient>();
            services.AddScoped<IPromotionsModuleClient>(_ => PromotionsMock.Object);

            services.RemoveAll<IGoodsReceiptModuleClient>();
            services.AddScoped<IGoodsReceiptModuleClient>(_ => GoodsReceiptMock.Object);

            services.RemoveAll<IReportingModuleClient>();
            services.AddScoped<IReportingModuleClient>(_ => ReportingMock.Object);
        });
    }
}

/// <summary>
/// Anonymous (unauthenticated) test factory for M2PortalBff.
/// JwtBearer remains active with fake AzureAd config — requests without a real Bearer token
/// get no identity, causing RequireAuthorization() to respond 401.
/// Use this factory exclusively for testing that endpoints reject unauthenticated callers.
/// </summary>
public sealed class PortalBffAnonFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:Audience"] = "api://test",
                ["Platform:BaseUrl"] = "https://platform-test.invalid",
                ["Platform:ApiKey"] = "test-api-key",
                ["Platform:InternalCallSecret"] = "internal",
                ["SapConnector:BaseUrl"] = "https://sap-test.invalid",
                ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL DbContext with in-memory.
            services.RemoveAll<DbContextOptions<M2DbContext>>();
            var inMemorySp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<M2DbContext>(opts =>
                opts.UseInMemoryDatabase($"PortalBffAnonDb-{Guid.NewGuid()}")
                    .UseInternalServiceProvider(inMemorySp));

            // No TestAuthHandler override — JwtBearer stays active.
            // Requests without an Authorization: Bearer header receive 401 from RequireAuthorization().
        });
    }
}

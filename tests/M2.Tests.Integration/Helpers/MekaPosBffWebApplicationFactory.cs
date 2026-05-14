using M2.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace M2.Tests.Integration.Helpers;

/// <summary>
/// Authenticated test factory for M2MekaPosBff.
/// TestAuthHandler replaces Entra ID JWT — all requests are authenticated as TestUserId.
/// Since SalesEndpoints and GoodsReceiptEndpoints call IAuthorizationService internally,
/// a fully empty InMemory DB means authz returns Deny → handlers return 403.
/// Use this factory for 403 and for tests where the auth middleware itself is not the concern.
/// </summary>
public class MekaPosBffWebApplicationFactory : WebApplicationFactory<M2.MekaPosBff.Program>
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
                opts.UseInMemoryDatabase($"MekaPosBffTestDb-{Guid.NewGuid()}")
                    .UseInternalServiceProvider(inMemorySp));

            // Replace Entra ID JWT with test scheme — all requests authenticated as TestUserId.
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

/// <summary>
/// Anonymous (unauthenticated) test factory for M2MekaPosBff.
/// JwtBearer remains active — requests without a Bearer token receive 401 from
/// the RequireAuthorization() group added to the /api/v1 route group.
/// Use this factory exclusively for testing that endpoints reject unauthenticated callers.
/// </summary>
public sealed class MekaPosBffAnonFactory : WebApplicationFactory<M2.MekaPosBff.Program>
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
                opts.UseInMemoryDatabase($"MekaPosBffAnonDb-{Guid.NewGuid()}")
                    .UseInternalServiceProvider(inMemorySp));

            // No TestAuthHandler override — JwtBearer stays active.
            // Requests without an Authorization: Bearer header receive 401 from RequireAuthorization().
        });
    }
}

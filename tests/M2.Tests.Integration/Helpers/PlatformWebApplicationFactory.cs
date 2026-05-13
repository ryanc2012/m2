using Hangfire;
using M2.Domain.Approvals;
using M2.Infrastructure;
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
/// Test factory for M2.Platform.Api — spins up the platform process in-memory for integration tests.
/// Module endpoint tests should use this factory, not the BFF factory.
/// </summary>
public class PlatformWebApplicationFactory : WebApplicationFactory<M2.Platform.Api.Program>
{
    public Mock<IApprovalService> ApprovalServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Development" is required so InfrastructureServiceExtensions suppresses the
        // Firebase ADC exception (catch clause guards on environment.IsDevelopment()).
        builder.UseEnvironment("Development");

        // Remove DevSeedService so it doesn't run during test host startup.
        // (It's only registered when environment.IsDevelopment(), but we need Development
        //  above to bypass the Firebase catch clause.)
        builder.ConfigureServices(services =>
        {
            var devSeed = services.FirstOrDefault(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                d.ImplementationType == typeof(M2.Infrastructure.Seed.DevSeedService));
            if (devSeed != null) services.Remove(devSeed);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Fake Azure AD values — actual token validation is bypassed by test auth scheme
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:Audience"] = "api://test",
                // Platform API key header used for inter-service calls
                ["Platform:ApiKey"] = "test-api-key",
                ["Platform:InternalCallSecret"] = "internal",
                // SAP connector — no real SAP needed (NoOp implementations registered by AddSapConnector)
                ["SapConnector:BaseUrl"] = "https://sap-test.invalid",
                // Fake connection string — valid format so PostgreSqlStorage ctor does not throw;
                // replaced with in-memory DB in ConfigureTestServices below
                ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL DbContext with in-memory for test isolation.
            // UseInternalServiceProvider isolates EF's provider services so Npgsql and InMemory
            // service registrations from AddInfrastructure don't collide in the same service provider.
            services.RemoveAll<DbContextOptions<M2DbContext>>();
            var inMemorySp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<M2DbContext>(opts =>
                opts.UseInMemoryDatabase($"M2PlatformTestDb-{Guid.NewGuid()}")
                    .UseInternalServiceProvider(inMemorySp));

            // Replace Azure AD JWT auth with a test scheme that accepts all requests
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Inject the controllable IApprovalService mock
            services.RemoveAll<IApprovalService>();
            services.AddScoped<IApprovalService>(_ => ApprovalServiceMock.Object);

            // Override PostgreSQL Hangfire storage with in-memory so RecurringJob.AddOrUpdate
            // in Program.cs does not fail during test host startup.
            services.AddHangfire(config => config.UseInMemoryStorage());
        });
    }
}

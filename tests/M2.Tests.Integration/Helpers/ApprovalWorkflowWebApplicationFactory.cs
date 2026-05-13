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
/// Factory for approval workflow integration tests.
/// Unlike PlatformWebApplicationFactory, this keeps the REAL IApprovalService wired to EF in-memory
/// so the full create → approve/reject/escalate chain can be exercised end-to-end.
/// IApprovalPolicyService is mocked so tests can control MaxLevels per scenario without touching
/// the static-dictionary implementation.
/// </summary>
public sealed class ApprovalWorkflowWebApplicationFactory
    : WebApplicationFactory<M2.Platform.Api.Program>
{
    private readonly string _dbName = $"ApprovalWorkflowTestDb-{Guid.NewGuid()}";

    public Mock<IApprovalPolicyService> PolicyServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Development" is required so InfrastructureServiceExtensions suppresses the
        // Firebase ADC exception (catch clause guards on environment.IsDevelopment()).
        builder.UseEnvironment("Development");

        // Remove DevSeedService so it doesn't run during test host startup.
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
                ["AzureAd:TenantId"]              = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"]              = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Instance"]              = "https://login.microsoftonline.com/",
                ["AzureAd:Audience"]              = "api://test",
                ["Platform:ApiKey"]               = "test-api-key",
                ["Platform:InternalCallSecret"]   = "internal",
                ["SapConnector:BaseUrl"]          = "https://sap-test.invalid",
                // Valid-format but fake connection string — PostgreSqlStorage ctor requires non-empty;
                // the actual DbContext is replaced with in-memory in ConfigureTestServices below.
                ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL with isolated InMemory EF.
            // Using UseInternalServiceProvider avoids the "two database providers" error:
            // AddInfrastructure registers Npgsql services into ASP.NET Core DI, and simply calling
            // AddDbContext(UseInMemoryDatabase) adds InMemory services on top — both providers end up
            // in the outer container and EF throws when it tries to build its internal SP.
            // UseInternalServiceProvider(inMemorySp) tells EF to use an isolated SP instead.
            services.RemoveAll<DbContextOptions<M2DbContext>>();
            var inMemorySp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<M2DbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName)
                    .UseInternalServiceProvider(inMemorySp));

            // Bypass Azure AD — all requests authenticated as TestAuthHandler.TestUserId
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Replace policy service with a controllable mock (avoids static-dict isolation issues).
            // Default: returns null (no policy) → ApprovalService defaults to MaxLevels=2.
            services.RemoveAll<IApprovalPolicyService>();
            services.AddScoped<IApprovalPolicyService>(_ => PolicyServiceMock.Object);

            // IApprovalService is NOT replaced — real EF-backed ApprovalService is used.

            // Override PostgreSQL Hangfire storage with in-memory so RecurringJob.AddOrUpdate
            // in Program.cs does not fail during test host startup.
            services.AddHangfire(config => config.UseInMemoryStorage());
        });
    }

    /// <summary>
    /// Creates an HttpClient pre-configured with the internal-call and API-key headers
    /// required by /modules/approvals/* endpoints.
    /// </summary>
    public HttpClient CreatePlatformClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
        client.DefaultRequestHeaders.Add("X-Internal-Call", "true");
        client.DefaultRequestHeaders.Add("X-Internal-Secret", "internal");
        return client;
    }

    /// <summary>
    /// Configures the policy mock to return a policy with the given MaxLevels for any tenant/entityType.
    /// Call before the test action that exercises approval steps.
    /// </summary>
    public void SetDefaultPolicy(int maxLevels)
    {
        var policy = new ApprovalPolicy(
            Guid.Empty, Guid.Empty, string.Empty,
            ApprovalMode.StepByStepPosition, maxLevels);

        PolicyServiceMock
            .Setup(p => p.GetPolicyAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ApprovalPolicy?>(policy));
    }
}

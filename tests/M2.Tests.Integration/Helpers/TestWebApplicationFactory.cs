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
/// Replaces external dependencies (Azure AD, PostgreSQL, SAP) with test-safe equivalents.
/// Uses EF Core In-Memory DB and a test auth scheme so no real infrastructure is required.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IApprovalService> ApprovalServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Fake Azure AD values — actual token validation is bypassed by test auth scheme
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:Audience"] = "api://test",
                // SAP connector — no real SAP needed (NoOp implementations registered by AddSapConnector)
                ["SapConnector:BaseUrl"] = "https://sap-test.invalid",
                // Fake connection string — replaced with in-memory DB below
                ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL DbContext with in-memory for test isolation
            services.RemoveAll<DbContextOptions<M2DbContext>>();
            services.AddDbContext<M2DbContext>(opts =>
                opts.UseInMemoryDatabase($"M2TestDb-{Guid.NewGuid()}"));

            // Replace Azure AD JWT auth with a test scheme that accepts all requests
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Inject the controllable IApprovalService mock
            services.RemoveAll<IApprovalService>();
            services.AddScoped<IApprovalService>(_ => ApprovalServiceMock.Object);
        });
    }
}

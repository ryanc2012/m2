using M2.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace M2.Tests.Integration;

/// <summary>
/// Base class for BFF-level integration tests (health endpoints, auth flows, BFF routing).
/// For module endpoint tests, use <see cref="M2PlatformIntegrationTestBase"/> instead.
/// </summary>
public abstract class M2BffIntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected M2BffIntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // Standard internal-call headers so module endpoints recognise requests
        // as coming from within the same process (ADR-001 internal call convention).
        Client.DefaultRequestHeaders.Add("X-Internal-Call", "true");
        Client.DefaultRequestHeaders.Add("X-Internal-Secret", "internal");
    }

    public void Dispose() => Client?.Dispose();
}

/// <summary>
/// Base class for Platform module integration tests.
/// All /modules/{name}/ endpoint tests should inherit this class and declare
/// IClassFixture&lt;PlatformWebApplicationFactory&gt;.
/// </summary>
public abstract class M2PlatformIntegrationTestBase : IDisposable
{
    protected readonly PlatformWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected M2PlatformIntegrationTestBase(PlatformWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // Platform API key header for inter-service authentication
        Client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
        // Internal-call headers required by module endpoints (ADR-001)
        Client.DefaultRequestHeaders.Add("X-Internal-Call", "true");
        Client.DefaultRequestHeaders.Add("X-Internal-Secret", "internal");
    }

    public void Dispose() => Client?.Dispose();
}


using M2.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace M2.Tests.Integration;

/// <summary>
/// Base class for integration tests that exercise module HTTP endpoints.
/// Subclasses must declare IClassFixture&lt;TestWebApplicationFactory&gt; (or a derived factory)
/// to ensure proper test isolation (in-memory DB, fake auth, etc.).
///
/// Wires inter-module loopback automatically via WithInterModuleLoopback() so any
/// IXxxModuleClient typed HttpClients route through the TestServer rather than the network.
/// </summary>
public abstract class M2IntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected M2IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithInterModuleLoopback();
        Client = Factory.CreateClient();

        // Standard internal-call headers so module endpoints recognise requests
        // as coming from within the same process (ADR-001 internal call convention).
        // The secret value "internal" is the well-known test default; production
        // secrets are injected via environment config.
        Client.DefaultRequestHeaders.Add("X-Internal-Call", "true");
        Client.DefaultRequestHeaders.Add("X-Internal-Secret", "internal");
    }

    public void Dispose() => Client?.Dispose();
}

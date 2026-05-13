using M2.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace M2.Tests.Integration;

/// <summary>
/// Base class for integration tests that exercise module HTTP endpoints.
/// Subclasses must declare IClassFixture&lt;TestWebApplicationFactory&gt; (or a derived factory)
/// to ensure proper test isolation (in-memory DB, fake auth, etc.).
///
/// The base class uses the injected factory directly. Call
/// factory.WithInterModuleLoopback() at the fixture-configuration level (not per-test-instance)
/// for test classes that validate live module-to-module HTTP flows — i.e., when Platform.InterModule
/// typed IXxxModuleClient registrations exist. Creating a new derived factory per-test-instance
/// would start a new TestServer for every test, causing static initializer races.
/// </summary>
public abstract class M2IntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected M2IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // Standard internal-call headers so module endpoints recognise requests
        // as coming from within the same process (ADR-001 internal call convention).
        // The secret value "internal" is the well-known test default; production
        // secrets are injected via environment config.
        Client.DefaultRequestHeaders.Add("X-Internal-Call", "true");
        Client.DefaultRequestHeaders.Add("X-Internal-Secret", "internal");
    }

    public void Dispose() => Client?.Dispose();
}

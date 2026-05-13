using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace M2.Tests.Integration.Helpers;

/// <summary>
/// Wires the TestWebApplicationFactory's in-process HttpMessageHandler into typed
/// inter-module clients. Without this, typed HttpClient instances targeting localhost
/// would attempt real network connections instead of routing through the TestServer.
/// </summary>
public static class InterModuleTestHelper
{
    /// <summary>
    /// Replaces the primary HttpMessageHandler for all IXxxModuleClient registrations
    /// with the factory's in-process handler, so inter-module HTTP calls loop back
    /// through the same TestServer without hitting the network.
    ///
    /// Must be called on every factory that tests module-to-module flows (ADR-001).
    /// Safe to call when no module clients are registered yet — the method is a no-op
    /// until McManus wires Platform.InterModule typed clients.
    /// </summary>
    public static WebApplicationFactory<Program> WithInterModuleLoopback(
        this WebApplicationFactory<Program> factory)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var moduleClientDescriptors = services
                    .Where(d => d.ServiceType.IsInterface &&
                                d.ServiceType.Name.EndsWith("ModuleClient"))
                    .ToList();

                // No-op when Platform.InterModule typed clients haven't been wired yet.
                // Once McManus adds IXxxModuleClient registrations, this branch becomes active
                // and all inter-module handlers are replaced with the test server's handler.
                if (moduleClientDescriptors.Count == 0)
                    return;

                // factory.Server lazily starts the original TestServer (with all test
                // overrides: in-memory DB, fake auth, etc.) on first access here.
                var handler = factory.Server.CreateHandler();
                ReplaceModuleClientHandlers(services, handler, moduleClientDescriptors);
            });
        });
    }

    /// <summary>
    /// Finds all typed HttpClient registrations whose interface name ends with
    /// "ModuleClient" and overrides their primary handler with the test server's
    /// in-process handler.
    ///
    /// Convention (ADR-001): inter-module typed clients follow the pattern
    ///   services.AddHttpClient&lt;IXxxModuleClient, XxxModuleClient&gt;()
    /// which registers a ServiceDescriptor with ServiceType = typeof(IXxxModuleClient).
    ///
    /// When McManus adds a new module client, no changes are needed here — the naming
    /// convention drives automatic discovery.
    /// </summary>
    private static void ReplaceModuleClientHandlers(
        IServiceCollection services,
        HttpMessageHandler handler,
        IEnumerable<ServiceDescriptor> moduleClientDescriptors)
    {
        foreach (var descriptor in moduleClientDescriptors)
        {
            // Re-register the named HttpClient for this typed client, overriding
            // the primary handler so calls route to the in-process TestServer.
            // The named-client key matches what AddHttpClient<TInterface, TImpl>()
            // uses internally (the interface type name).
            services.AddHttpClient(descriptor.ServiceType.Name)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }
    }
}

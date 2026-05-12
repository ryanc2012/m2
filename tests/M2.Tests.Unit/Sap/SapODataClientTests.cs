using FluentAssertions;
using M2.Domain.Sap;
using M2.SapConnector;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;

namespace M2.Tests.Unit.Sap;

/// <summary>
/// Unit tests for SapODataClient, SapNcoClient, and SapOutboxEntry.
/// All network calls are mocked — no live SAP system used in CI (TEST-001).
/// </summary>
public class SapODataClientTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    /// <summary>Creates a SapODataClient wired to the supplied HttpMessageHandler.</summary>
    private static SapODataClient BuildODataClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://sap-test/") };
        var configMock = new Mock<IConfiguration>();
        // Return empty base URL so the constructor does not overwrite our test BaseAddress.
        configMock.Setup(c => c["Sap:ODataBaseUrl"]).Returns(string.Empty);
        return new SapODataClient(httpClient, configMock.Object);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsFailureOnHttpError()
    {
        // When the HTTP layer throws (network error, timeout) the client must absorb the
        // exception and return Result.Failure — callers must never receive an unhandled exception.
        var client = BuildODataClient(new ThrowingHttpMessageHandler());

        var result = await client.GetProductsAsync(_tenantId);

        result.IsFailure.Should().BeTrue(
            "GetProductsAsync must return Result.Failure when the HTTP call throws, not propagate the exception");
        result.Error.Should().NotBeNullOrEmpty(
            "the failure result must carry a non-empty error message for diagnostics");
    }

    [Fact]
    public async Task PostGoodsMovementAsync_ReturnsFailureOnHttpError()
    {
        // Same resilience contract for POST — critical write path must not throw to callers.
        var client = BuildODataClient(new ThrowingHttpMessageHandler());
        var payload = new SapGoodsMovementPayload(
            DeliveryNoteNumber: "DN-TEST-001",
            MovementType: "101",
            Items: Array.Empty<SapGoodsMovementItem>());

        var result = await client.PostGoodsMovementAsync(_tenantId, payload);

        result.IsFailure.Should().BeTrue(
            "PostGoodsMovementAsync must return Result.Failure on HTTP error — the outbox worker will retry");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SapNcoClient_ThrowsNotSupported()
    {
        // ADR-006: NCo is the fallback channel. Without native DLLs it must throw
        // NotSupportedException immediately — not silently return empty data.
        var client = new SapNcoClient();

        // Use Action (not Func<Task>) so synchronous throw is caught by FluentAssertions.
        Action act = () => client.GetProductsAsync(_tenantId);

        act.Should().Throw<NotSupportedException>(
            "SapNcoClient requires native SAP RFC DLLs unavailable in CI — it must throw " +
            "NotSupportedException so misconfigured environments fail fast instead of returning silent empty data");
    }

    [Fact]
    public void SapOutboxEntry_StartsAsPending()
    {
        // All new outbox entries must start Pending so the Hangfire worker picks them up.
        var entry = new SapOutboxEntry(_tenantId, _shopId, "GoodsMovement", "{\"dn\":\"DN-001\"}");

        entry.Status.Should().Be(SapOutboxStatus.Pending,
            "a newly created outbox entry must have Status=Pending so the worker processes it");
        entry.RetryCount.Should().Be(0,
            "retry count must start at 0 — no attempts have been made yet");
        entry.ProcessedAt.Should().BeNull(
            "ProcessedAt must be null until the entry is successfully sent to SAP");
        entry.ErrorMessage.Should().BeNull(
            "ErrorMessage must be null for a freshly created entry");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>HttpMessageHandler that always throws HttpRequestException.</summary>
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure — no SAP in CI.");
    }
}

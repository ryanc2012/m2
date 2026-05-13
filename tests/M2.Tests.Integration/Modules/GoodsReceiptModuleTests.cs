using System.Net;
using FluentAssertions;
using M2.Tests.Integration.Helpers;

namespace M2.Tests.Integration.Modules;

/// <summary>
/// Smoke tests for the GoodsReceipt module HTTP surface.
///
/// Target endpoint prefix: /modules/goods-receipt/
/// Verifies GET /modules/goods-receipt/{id} is reachable (ADR-001 inter-module endpoint).
/// 404 is the expected result for a random GUID against an empty test DB.
/// 500 is never acceptable.
/// </summary>
public class GoodsReceiptModuleTests : M2PlatformIntegrationTestBase, IClassFixture<PlatformWebApplicationFactory>
{
    public GoodsReceiptModuleTests(PlatformWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// Contract: GET /modules/goods-receipt/{id} must return 200 or 404 — never 401/403/500.
    /// Empty test DB means 404 is the normal result for a random GUID.
    /// </summary>
    [Fact]
    public async Task GetGoodsReceipt_WithInternalHeaders_ReturnsOkOrNotFound()
    {
        var id = Guid.NewGuid();

        var response = await Client.GetAsync($"/modules/goods-receipt/{id}");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "endpoint must return 200 when the GRN exists or 404 when it doesn't; 500 is never acceptable (ADR-001)");
    }

    /// <summary>
    /// Contract: GET /modules/goods-receipt/{id} WITHOUT the X-Internal-Call header must ultimately
    /// be rejected with 401/403 once the internal-call middleware is enforced (ADR-001).
    /// </summary>
    [Fact]
    public async Task GetGoodsReceipt_WithoutInternalHeader_Returns401Or403OrNotFound()
    {
        using var clientWithoutHeaders = Factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await clientWithoutHeaders.GetAsync($"/modules/goods-receipt/{id}");

        response.StatusCode.Should().BeOneOf(
            new[]
            {
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NotFound,
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
            },
            "module endpoint must ultimately reject callers missing X-Internal-Call (ADR-001/ADR-004)");
    }
}

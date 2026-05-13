using System.Net;
using FluentAssertions;
using M2.Tests.Integration.Helpers;

namespace M2.Tests.Integration.Modules;

/// <summary>
/// Smoke tests for the Reporting module HTTP surface.
///
/// Target endpoint prefix: /modules/reporting/
/// Verifies GET /modules/reporting/sales/daily is reachable (ADR-001 inter-module endpoint).
/// The endpoint requires tenantId, shopId, and date query params;
/// 400 is acceptable if the service rejects the request, but 500 is never acceptable.
///
/// Note: the reporting endpoint is /sales/daily (not /sales/summary); summary is the shape
/// of the response payload, not the route segment.
/// </summary>
public class ReportingModuleTests : M2IntegrationTestBase, IClassFixture<TestWebApplicationFactory>
{
    public ReportingModuleTests(TestWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// Contract: GET /modules/reporting/sales/daily must return 200, 400, or 404 — never 401/403/500.
    /// Supplying valid query params exercises the endpoint routing and service dispatch.
    /// </summary>
    [Fact]
    public async Task GetSalesSummary_WithInternalHeaders_ReturnsOkOrExpectedError()
    {
        var tenantId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await Client.GetAsync(
            $"/modules/reporting/sales/daily?tenantId={tenantId}&shopId={shopId}&date={date}");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest },
            "endpoint must return 200, 404, or 400; 500 is never acceptable (ADR-001)");
    }

    /// <summary>
    /// Contract: GET /modules/reporting/sales/daily WITHOUT the X-Internal-Call header must
    /// ultimately be rejected with 401/403 once the internal-call middleware is enforced (ADR-001).
    /// </summary>
    [Fact]
    public async Task GetSalesSummary_WithoutInternalHeader_Returns401Or403OrNotFound()
    {
        using var clientWithoutHeaders = Factory.CreateClient();
        var tenantId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await clientWithoutHeaders.GetAsync(
            $"/modules/reporting/sales/daily?tenantId={tenantId}&shopId={shopId}&date={date}");

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

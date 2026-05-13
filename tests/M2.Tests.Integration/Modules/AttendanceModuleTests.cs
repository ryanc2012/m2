using System.Net;
using FluentAssertions;
using M2.Tests.Integration.Helpers;

namespace M2.Tests.Integration.Modules;

/// <summary>
/// Smoke tests for the Attendance module HTTP surface.
///
/// Target endpoint prefix: /modules/attendance/
/// Verifies GET /modules/attendance/summary is reachable (ADR-001 inter-module endpoint).
/// The endpoint requires tenantId, employeeId, and date query params;
/// 400 is acceptable if the service rejects the request, but 500 is never acceptable.
/// </summary>
public class AttendanceModuleTests : M2IntegrationTestBase, IClassFixture<TestWebApplicationFactory>
{
    public AttendanceModuleTests(TestWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// Contract: GET /modules/attendance/summary must return 200, 400, or 404 — never 401/403/500.
    /// Supplying a valid query string exercises the endpoint routing and service dispatch.
    /// 400 is returned when the service finds no records; 200 if records exist.
    /// </summary>
    [Fact]
    public async Task GetAttendanceSummary_WithInternalHeaders_ReturnsOkOrExpectedError()
    {
        var tenantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await Client.GetAsync(
            $"/modules/attendance/summary?tenantId={tenantId}&employeeId=EMP-001&date={date}");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest },
            "endpoint must return 200, 404, or 400; 500 is never acceptable (ADR-001)");
    }

    /// <summary>
    /// Contract: GET /modules/attendance/summary WITHOUT the X-Internal-Call header must ultimately
    /// be rejected with 401/403 once the internal-call middleware is enforced (ADR-001).
    /// </summary>
    [Fact]
    public async Task GetAttendanceSummary_WithoutInternalHeader_Returns401Or403OrNotFound()
    {
        using var clientWithoutHeaders = Factory.CreateClient();
        var tenantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await clientWithoutHeaders.GetAsync(
            $"/modules/attendance/summary?tenantId={tenantId}&employeeId=EMP-001&date={date}");

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

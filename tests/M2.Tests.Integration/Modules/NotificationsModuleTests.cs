using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using M2.Tests.Integration.Helpers;

namespace M2.Tests.Integration.Modules;

/// <summary>
/// Smoke tests for the Notifications module HTTP surface.
///
/// Target endpoint prefix: /modules/notifications/
/// These endpoints are inter-module REST calls (ADR-001) distinct from the BFF-facing
/// /notifications/* endpoints. McManus is implementing them concurrently; HTTP 404 is
/// acceptable until they are wired up. The contracts below are final.
///
/// Once implemented, the endpoints must:
///   - Accept X-Internal-Call / X-Internal-Secret headers for module-to-module calls
///   - Reject requests missing those headers with 401 or 403
/// </summary>
public class NotificationsModuleTests : M2IntegrationTestBase, IClassFixture<TestWebApplicationFactory>
{
    public NotificationsModuleTests(TestWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// Contract: GET /modules/notifications/history/member/{memberId} must return 200 or 404
    /// (never 401/403/500). 404 is acceptable while the endpoint is not yet registered.
    /// </summary>
    [Fact]
    public async Task GetNotificationHistory_ReturnsOkOrNotFound()
    {
        var memberId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var response = await Client.GetAsync(
            $"/modules/notifications/history/member/{memberId}?tenantId={tenantId}&page=1&pageSize=10");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "endpoint returns 200 when implemented; 404 is acceptable during parallel dev (ADR-001)");
    }

    /// <summary>
    /// Contract: POST /modules/notifications/send WITHOUT the X-Internal-Call header
    /// must be rejected with 401 or 403. HTTP 404 is acceptable while the endpoint is
    /// not yet registered (request will never reach auth middleware on a 404 path).
    /// </summary>
    [Fact]
    public async Task SendNotification_WithoutInternalHeader_Returns401Or403OrNotFound()
    {
        using var unauthenticatedClient = Factory.CreateClient();
        // Deliberately omit X-Internal-Call and X-Internal-Secret headers

        var response = await unauthenticatedClient.PostAsJsonAsync(
            "/modules/notifications/send",
            new { });

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
            "module send endpoint must reject unauthenticated callers (ADR-001/ADR-004); " +
            "404 acceptable until endpoint is registered");
    }

    /// <summary>
    /// Contract: POST /modules/notifications/send WITH valid internal headers must return
    /// 200/202/404 (not 401/403/500). Verifies the bypass logic works when headers are present.
    /// </summary>
    [Fact]
    public async Task SendNotification_WithInternalHeaders_IsAcceptedOrNotFound()
    {
        // Client already has X-Internal-Call + X-Internal-Secret from base class
        var response = await Client.PostAsJsonAsync(
            "/modules/notifications/send",
            new { });

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.BadRequest, HttpStatusCode.NotFound },
            "endpoint should accept internal-header calls and not return 401/403/500");
    }
}

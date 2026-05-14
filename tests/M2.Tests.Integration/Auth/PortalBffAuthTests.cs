using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using M2.Domain.Approvals.Dtos;
using M2.SharedKernel;
using M2.Tests.Integration.Helpers;
using Moq;

namespace M2.Tests.Integration.Auth;

/// <summary>
/// Integration tests for M2PortalBff authentication/authorisation at the HTTP boundary.
/// S5.10 — covers 401 (unauthenticated) and health-check exemption; 403 tests are skipped
/// pending S5.3 (auth-object enforcement inside PortalBff endpoint handlers).
///
/// Factory split:
///   PortalBffWebApplicationFactory  — TestAuthHandler (all requests authenticated)
///   PortalBffAnonFactory            — real JwtBearer with no token → 401
/// </summary>
public class PortalBffAuthTests :
    IClassFixture<PortalBffWebApplicationFactory>,
    IClassFixture<PortalBffAnonFactory>
{
    private readonly PortalBffWebApplicationFactory _authedFactory;
    private readonly HttpClient _authedClient;
    private readonly HttpClient _anonClient;

    private static readonly Guid TenantId = WellKnownTenants.Default;
    private static readonly Guid ShopId = new("22222222-2222-2222-2222-222222222222");

    public PortalBffAuthTests(
        PortalBffWebApplicationFactory authedFactory,
        PortalBffAnonFactory anonFactory)
    {
        _authedFactory = authedFactory;
        _authedClient = authedFactory.CreateClient();
        _anonClient = anonFactory.CreateClient();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingApprovals_WithValidToken_Returns200()
    {
        // Arrange: mock the approvals module client to return an empty list.
        _authedFactory.ApprovalsMock
            .Setup(c => c.GetPendingRequestsForApproverAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ApprovalRequestDto>>(
                Array.Empty<ApprovalRequestDto>()));

        // Act
        var response = await _authedClient.GetAsync(
            "/api/v1/approvals/pending?approverId=mgr-001");

        // Assert — auth passed; downstream result controls the status code
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "an authenticated request to a protected endpoint must not be rejected by auth middleware");
    }

    // ── 401 — unauthenticated callers ─────────────────────────────────────────

    [Fact]
    public async Task GetPendingApprovals_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync(
            "/api/v1/approvals/pending?approverId=mgr-001");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET /api/v1/approvals/pending is in the RequireAuthorization() group and must reject unauthenticated requests");
    }

    [Fact]
    public async Task GetReporting_WithoutToken_Returns401()
    {
        // Reporting is mapped inside the secured group as /api/v1/api/reports/...
        // The double 'api' segment is how MapReportingEndpoints nests its group.
        var response = await _anonClient.GetAsync(
            $"/api/v1/api/reports/sales/daily?tenantId={TenantId}&shopId={ShopId}&date=2025-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "reporting endpoints must reject unauthenticated callers");
    }

    [Fact]
    public async Task PostGoodsReceipt_WithoutToken_Returns401()
    {
        var response = await _anonClient.PostAsync("/api/v1/goods-receipts", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "POST /api/v1/goods-receipts is in the RequireAuthorization() group");
    }

    [Fact]
    public async Task PostApprovalRequest_WithoutToken_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync(
            "/api/v1/approvals/requests",
            new { entityType = "Promotion", entityId = Guid.NewGuid(), tenantId = TenantId, shopId = ShopId });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "POST /api/v1/approvals/requests must reject unauthenticated callers");
    }

    // ── Health — exempt from authentication ───────────────────────────────────

    [Fact]
    public async Task GetHealth_WithoutToken_Returns200()
    {
        // /health/live is mapped OUTSIDE the secured /api/v1 group with Predicate = _ => false
        // (no dependency checks), so it always returns 200 regardless of downstream connectivity.
        var response = await _anonClient.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "health/live endpoint must be exempt from authentication and return 200");
    }

    // ── 403 — authenticated but missing authObject (pending S5.3) ────────────

    [Fact(Skip = "Pending S5.3: PromotionEndpoints in PortalBff has no IAuthorizationService check yet — McManus Wave 2")]
    public async Task PostPromotion_WithValidToken_ButNoRole_Returns403()
    {
        // When PortalBff promotion handler calls IAuthorizationService.CheckAsync("M_PROMOTION_MANAGE")
        // and the InMemory DB has no role for TestUserId, the handler returns Results.Forbid() → 403.
        var response = await _authedClient.PostAsJsonAsync(
            "/api/v1/promotions/",
            new
            {
                tenantId = TenantId, shopId = ShopId,
                nameEn = "Test", nameZht = "測試",
                type = "PercentageDiscount",
                formulaJson = "{}", startDate = DateTimeOffset.UtcNow,
                endDate = DateTimeOffset.UtcNow.AddDays(30), isStackable = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an authenticated user without M_PROMOTION_MANAGE must receive 403");
    }

    [Fact(Skip = "Pending S5.3: PortalBff ApprovalEndpoints has no IAuthorizationService check yet — McManus Wave 2")]
    public async Task ApproveRequest_WithValidToken_ButNoRole_Returns403()
    {
        // When PortalBff approval handler calls IAuthorizationService.CheckAsync("M_APPROVAL_PROCESS"),
        // the handler returns Results.Forbid() → 403.
        var response = await _authedClient.PostAsJsonAsync(
            $"/api/v1/approvals/requests/{Guid.NewGuid()}/approve",
            new { approverId = "mgr-001", comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an authenticated user without M_APPROVAL_PROCESS must receive 403");
    }

    [Fact(Skip = "Pending S5.3: PortalBff ApprovalEndpoints has no IAuthorizationService check yet — McManus Wave 2")]
    public async Task RejectRequest_WithValidTokenAndPermit_Returns2xx()
    {
        // After S5.3: seed a UserRoleAssignment for TestUserId with M_APPROVAL_PROCESS,
        // then verify the handler proceeds past the auth check.
        var response = await _authedClient.PostAsJsonAsync(
            $"/api/v1/approvals/requests/{Guid.NewGuid()}/reject",
            new { approverId = "mgr-001", comment = "budget exceeded" });

        ((int)response.StatusCode).Should().BeInRange(200, 299,
            "a user with the correct role must get past the auth check");
    }
}

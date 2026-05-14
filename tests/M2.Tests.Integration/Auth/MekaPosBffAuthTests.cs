using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using M2.Tests.Integration.Helpers;

namespace M2.Tests.Integration.Auth;

/// <summary>
/// Integration tests for M2MekaPosBff authentication/authorisation at the HTTP boundary.
/// S5.10 — covers 401 (unauthenticated), 403 (authenticated but no authObject), and
/// health-check exemption.
///
/// S5.3 for MekaPosBff is already implemented: SalesEndpoints and GoodsReceiptEndpoints
/// call IAuthorizationService internally, so 403 tests run without skipping.
///
/// Factory split:
///   MekaPosBffWebApplicationFactory — TestAuthHandler (all requests authenticated)
///   MekaPosBffAnonFactory           — real JwtBearer with no token → 401
/// </summary>
public class MekaPosBffAuthTests :
    IClassFixture<MekaPosBffWebApplicationFactory>,
    IClassFixture<MekaPosBffAnonFactory>
{
    private readonly HttpClient _authedClient;
    private readonly HttpClient _anonClient;

    public MekaPosBffAuthTests(
        MekaPosBffWebApplicationFactory authedFactory,
        MekaPosBffAnonFactory anonFactory)
    {
        _authedClient = authedFactory.CreateClient();
        _anonClient = anonFactory.CreateClient();
    }

    // ── 401 — unauthenticated callers ─────────────────────────────────────────

    [Fact]
    public async Task PostSales_WithoutToken_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync(
            "/api/v1/sales/transactions",
            new
            {
                tenantId = Guid.NewGuid(), shopId = Guid.NewGuid(),
                cashierId = "EMP001", paymentMethod = "Cash",
                lineItems = Array.Empty<object>()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "POST /api/v1/sales/transactions is in the RequireAuthorization() group");
    }

    [Fact]
    public async Task GetAttendance_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync(
            $"/api/v1/attendance/records?tenantId={Guid.NewGuid()}&employeeId=EMP001");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET /api/v1/attendance/records is in the RequireAuthorization() group");
    }

    [Fact]
    public async Task PostGoodsReceipt_WithoutToken_Returns401()
    {
        var response = await _anonClient.PostAsync("/api/v1/goods-receipts", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "POST /api/v1/goods-receipts is in the RequireAuthorization() group");
    }

    // ── Health — exempt from authentication ───────────────────────────────────

    [Fact]
    public async Task GetHealth_WithoutToken_Returns200()
    {
        // /health/live is mapped outside the secured /api/v1 group with Predicate = _ => false
        // (no dependency checks), so it always returns 200 regardless of platform connectivity.
        var response = await _anonClient.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "health/live endpoint must be exempt from authentication and return 200");
    }

    // ── 403 — authenticated but missing authObject ────────────────────────────

    [Fact]
    public async Task PostSalesTransaction_AuthenticatedButNoRole_Returns403()
    {
        // Use the /void route — no request body, only a route param, so model binding
        // always succeeds. The handler calls authz.CheckAsync("M_SALES_VOID") first.
        // InMemory DB has no role for TestUserId → Deny → Results.Forbid() → 403.
        var txId = Guid.NewGuid();
        var response = await _authedClient.PostAsync(
            $"/api/v1/sales/transactions/{txId}/void", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an authenticated user without M_SALES_VOID must receive 403 from the handler auth check");
    }

    [Fact]
    public async Task PostGoodsReceipt_AuthenticatedButNoRole_Returns403()
    {
        // GoodsReceiptEndpoints checks M_GOODS_RECEIPT_CREATE before calling the module client.
        // InMemory DB has no role for TestUserId → Deny → Results.Forbid() → 403.
        var response = await _authedClient.PostAsJsonAsync(
            "/api/v1/goods-receipts",
            new
            {
                tenantId = Guid.NewGuid(), shopId = Guid.NewGuid(),
                sapDeliveryNoteNumber = "4500001234",
                lineItems = Array.Empty<object>()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an authenticated user without M_GOODS_RECEIPT_CREATE must receive 403 from the handler auth check");
    }
}

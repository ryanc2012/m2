using M2.Domain.Approvals;
using M2.Domain.Members;
using M2.Domain.Promotions;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Seed;

/// <summary>
/// Development-only startup service that seeds baseline fixture data idempotently.
/// McManus registers this in InfrastructureServiceExtensions.cs via
///   services.AddHostedService&lt;DevSeedService&gt;() guarded by env.IsDevelopment().
/// </summary>
public sealed class DevSeedService : IHostedService
{
    private static readonly Guid DevShopId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DevSeedService> _logger;

    public DevSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<DevSeedService> logger)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<M2DbContext>();

        _logger.LogInformation("[DevSeed] Starting development seed for tenant {TenantId}", WellKnownTenants.Default);

        await SeedMembersAsync(db, cancellationToken);
        await SeedPromotionsAsync(db, cancellationToken);
        await SeedApprovalPoliciesAsync(db, cancellationToken);

        _logger.LogInformation("[DevSeed] Seed complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Members ──────────────────────────────────────────────────────────────

    private static async Task SeedMembersAsync(M2DbContext db, CancellationToken ct)
    {
        if (await db.Members.AnyAsync(m => m.TenantId == WellKnownTenants.Default, ct))
            return;

        var members = new[]
        {
            new Member(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Alice", "愛麗絲"), BilingualText.From("Chan", "陳"),
                "+85291000001", "alice.chan@example.com", MembershipTier.Gold),

            new Member(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Bob", "寶寶"), BilingualText.From("Lee", "李"),
                "+85291000002", "bob.lee@example.com", MembershipTier.Silver),

            new Member(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Carol", "嘉禮"), BilingualText.From("Wong", "黃"),
                "+85291000003", "carol.wong@example.com", MembershipTier.Standard),

            new Member(WellKnownTenants.Default, DevShopId,
                BilingualText.From("David", "大衛"), BilingualText.From("Ng", "吳"),
                "+85291000004", null, MembershipTier.Platinum),

            new Member(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Eva", "伊娃"), BilingualText.From("Lam", "林"),
                "+85291000005", "eva.lam@example.com", MembershipTier.Standard),
        };

        db.Members.AddRange(members);
        await db.SaveChangesAsync(ct);
    }

    // ── Promotions ────────────────────────────────────────────────────────────

    private static async Task SeedPromotionsAsync(M2DbContext db, CancellationToken ct)
    {
        if (await db.Promotions.AnyAsync(p => p.TenantId == WellKnownTenants.Default, ct))
            return;

        var now = DateTimeOffset.UtcNow;

        var promotions = new[]
        {
            CreateActivePromotion(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Summer 10% Off", "夏日九折優惠"),
                PromotionType.PercentDiscount,
                """{"percentage": 10}""",
                now.AddDays(-7), now.AddDays(30)),

            CreateActivePromotion(WellKnownTenants.Default, DevShopId,
                BilingualText.From("Buy 2 Get 1 Free", "買二送一"),
                PromotionType.BuyXGetY,
                """{"buyQty": 2, "getQty": 1}""",
                now.AddDays(-3), now.AddDays(14)),

            CreateActivePromotion(WellKnownTenants.Default, DevShopId,
                BilingualText.From("$50 Fixed Discount", "折扣$50"),
                PromotionType.FixedDiscount,
                """{"amount": 50, "currency": "HKD"}""",
                now, now.AddDays(7)),
        };

        db.Promotions.AddRange(promotions);
        await db.SaveChangesAsync(ct);
    }

    private static Promotion CreateActivePromotion(
        Guid tenantId, Guid shopId,
        BilingualText name, PromotionType type,
        string formulaJson,
        DateTimeOffset start, DateTimeOffset end)
    {
        var p = new Promotion(tenantId, shopId, name, type, formulaJson, start, end, isStackable: false);
        p.Activate();
        return p;
    }

    // ── Approval Policies ─────────────────────────────────────────────────────

    private static async Task SeedApprovalPoliciesAsync(M2DbContext db, CancellationToken ct)
    {
        if (await db.ApprovalPolicies.AnyAsync(a => a.TenantId == WellKnownTenants.Default, ct))
            return;

        var policies = new[]
        {
            new ApprovalPolicy(WellKnownTenants.Default, DevShopId,
                entityType: "Promotion",
                mode: ApprovalMode.StepByStepPosition,
                maxLevels: 2),

            new ApprovalPolicy(WellKnownTenants.Default, DevShopId,
                entityType: "GoodsReceipt",
                mode: ApprovalMode.SapHcmHierarchy,
                maxLevels: 3),
        };

        db.ApprovalPolicies.AddRange(policies);
        await db.SaveChangesAsync(ct);
    }
}

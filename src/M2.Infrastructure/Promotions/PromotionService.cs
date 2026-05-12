using M2.Domain.Promotions;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Promotions;

/// <summary>In-memory stub PromotionService. EF wiring deferred to Sprint 4.</summary>
internal sealed class PromotionService(
    ICouponService couponService,
    ILogger<PromotionService> logger) : IPromotionService
{
    private static readonly Dictionary<Guid, Promotion> _promotions = [];

    public Task<Result<Promotion>> CreateAsync(
        Guid tenantId, Guid shopId, BilingualText name, PromotionType type,
        string formulaJson, DateTimeOffset startDate, DateTimeOffset endDate,
        bool isStackable, CancellationToken ct = default)
    {
        var promotion = new Promotion(tenantId, shopId, name, type, formulaJson, startDate, endDate, isStackable);
        _promotions[promotion.Id] = promotion;

        logger.LogInformation(
            "Promotion created: id={Id} type={Type} tenant={TenantId}",
            promotion.Id, type, tenantId);

        return Task.FromResult(Result.Success(promotion));
    }

    public Task<Result<Promotion>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _promotions.TryGetValue(id, out var promotion)
            ? Task.FromResult(Result.Success(promotion))
            : Task.FromResult(Result.Failure<Promotion>($"Promotion {id} not found."));
    }

    /// <summary>
    /// Activates promotion and pre-issues coupons to all eligible members (ADR-013).
    /// Stub: issues one generic coupon as a placeholder for batch issuance.
    /// </summary>
    public async Task<Result<Promotion>> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        if (!_promotions.TryGetValue(id, out var promotion))
            return Result.Failure<Promotion>($"Promotion {id} not found.");

        promotion.Activate();

        // Pre-issue a generic coupon on activation (ADR-013)
        var expiry = promotion.EndDate;
        var code = $"PROMO-{id:N}".ToUpperInvariant();
        await couponService.IssueAsync(
            promotion.TenantId, promotion.ShopId, id, memberId: null, expiry, ct);

        logger.LogInformation("Promotion activated and coupons pre-issued: id={Id}", id);
        return Result.Success(promotion);
    }

    public Task<Result<Promotion>> PauseAsync(Guid id, CancellationToken ct = default)
    {
        if (!_promotions.TryGetValue(id, out var promotion))
            return Task.FromResult(Result.Failure<Promotion>($"Promotion {id} not found."));

        promotion.Pause();
        logger.LogInformation("Promotion paused: id={Id}", id);
        return Task.FromResult(Result.Success(promotion));
    }

    public Task<Result<IReadOnlyList<Promotion>>> GetActiveForShopAsync(
        Guid tenantId, Guid shopId, CancellationToken ct = default)
    {
        var active = _promotions.Values
            .Where(p => p.TenantId == tenantId && p.ShopId == shopId && p.Status == PromotionStatus.Active)
            .ToList() as IReadOnlyList<Promotion>;

        return Task.FromResult(Result.Success(active));
    }
}

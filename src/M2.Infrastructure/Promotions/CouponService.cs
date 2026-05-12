using M2.Domain.Promotions;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Promotions;

/// <summary>In-memory stub CouponService. EF wiring deferred to Sprint 4.</summary>
internal sealed class CouponService(ILogger<CouponService> logger) : ICouponService
{
    private static readonly Dictionary<string, Coupon> _coupons = [];

    public Task<Result<Coupon>> IssueAsync(
        Guid tenantId, Guid shopId, Guid promotionId, Guid? memberId,
        DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var code = $"{promotionId:N}-{Guid.NewGuid():N}".ToUpperInvariant()[..16];
        var coupon = new Coupon(tenantId, shopId, promotionId, memberId, code, expiresAt);
        _coupons[code] = coupon;

        logger.LogInformation(
            "Coupon issued: code={Code} promotionId={PromotionId} memberId={MemberId}",
            code, promotionId, memberId);

        return Task.FromResult(Result.Success(coupon));
    }

    public Task<Result<Coupon>> ValidateAsync(string code, CancellationToken ct = default)
    {
        if (!_coupons.TryGetValue(code.ToUpperInvariant(), out var coupon))
            return Task.FromResult(Result.Failure<Coupon>($"Coupon '{code}' not found."));

        if (coupon.IsRedeemed)
            return Task.FromResult(Result.Failure<Coupon>($"Coupon '{code}' has already been redeemed."));

        if (coupon.ExpiresAt < DateTimeOffset.UtcNow)
            return Task.FromResult(Result.Failure<Coupon>($"Coupon '{code}' has expired."));

        return Task.FromResult(Result.Success(coupon));
    }

    public async Task<Result<Coupon>> RedeemAsync(string code, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(code, ct);
        if (!validation.IsSuccess) return validation;

        var coupon = validation.Value!;
        coupon.Redeem();

        logger.LogInformation("Coupon redeemed: code={Code}", code);
        return Result.Success(coupon);
    }

    public Task<Result<IReadOnlyList<Coupon>>> GetForMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var coupons = _coupons.Values
            .Where(c => c.MemberId == memberId)
            .ToList() as IReadOnlyList<Coupon>;

        return Task.FromResult(Result.Success(coupons));
    }
}

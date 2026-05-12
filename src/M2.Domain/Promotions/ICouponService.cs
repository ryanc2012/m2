using M2.SharedKernel;

namespace M2.Domain.Promotions;

public interface ICouponService
{
    Task<Result<Coupon>> IssueAsync(
        Guid tenantId,
        Guid shopId,
        Guid promotionId,
        Guid? memberId,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);

    /// <summary>Validates a coupon code without redeeming it.</summary>
    Task<Result<Coupon>> ValidateAsync(string code, CancellationToken ct = default);

    /// <summary>Redeems a coupon at POS, marking it as used.</summary>
    Task<Result<Coupon>> RedeemAsync(string code, CancellationToken ct = default);

    Task<Result<IReadOnlyList<Coupon>>> GetForMemberAsync(Guid memberId, CancellationToken ct = default);
}

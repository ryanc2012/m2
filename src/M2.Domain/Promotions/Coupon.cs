using M2.SharedKernel;

namespace M2.Domain.Promotions;

public class Coupon : BaseEntity
{
    public Guid PromotionId { get; protected set; }
    /// <summary>Null for generic (non-member-specific) coupons.</summary>
    public Guid? MemberId { get; protected set; }
    public string Code { get; protected set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; protected set; }
    public DateTimeOffset? RedeemedAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public bool IsRedeemed { get; protected set; }

    protected Coupon() { }

    public Coupon(
        Guid tenantId,
        Guid shopId,
        Guid promotionId,
        Guid? memberId,
        string code,
        DateTimeOffset expiresAt)
    {
        TenantId = tenantId;
        ShopId = shopId;
        PromotionId = promotionId;
        MemberId = memberId;
        Code = code;
        ExpiresAt = expiresAt;
        IssuedAt = DateTimeOffset.UtcNow;
        IsRedeemed = false;
    }

    public void Redeem()
    {
        IsRedeemed = true;
        RedeemedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

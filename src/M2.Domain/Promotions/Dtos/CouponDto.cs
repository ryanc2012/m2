namespace M2.Domain.Promotions.Dtos;

public record CouponDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    Guid PromotionId,
    Guid? MemberId,
    string Code,
    DateTimeOffset IssuedAt,
    DateTimeOffset? RedeemedAt,
    DateTimeOffset ExpiresAt,
    bool IsRedeemed);

public record CalculateDiscountPayload(
    IReadOnlyList<CartItemDto> CartItems,
    Guid? MemberId,
    Guid ShopId);

public record CartItemDto(string ProductId, int Quantity, decimal UnitPrice);

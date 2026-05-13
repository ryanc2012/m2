using M2.Domain.Promotions;
using M2.Domain.Promotions.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IPromotionsModuleClient
{
    Task<Result<PromotionDto>> CreatePromotionAsync(CreatePromotionPayload payload, CancellationToken ct = default);
    Task<Result<PromotionDto>> GetPromotionByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromotionDto>> ActivatePromotionAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromotionDto>> PausePromotionAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PromotionDto>>> GetActivePromotionsForShopAsync(Guid tenantId, Guid shopId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CouponDto>>> GetCouponsByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<Result<CouponDto>> RedeemCouponAsync(string code, CancellationToken ct = default);

    Task<Result<DiscountResult>> CalculateDiscountsAsync(CalculateDiscountPayload payload, CancellationToken ct = default);
}

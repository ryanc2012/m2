using M2.Domain.Promotions;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Promotions;

/// <summary>
/// Stub discount engine. Applies active stackable promotions per ADR-020.
/// Full formula evaluation deferred to Sprint 4 (Discount Engine epic).
/// </summary>
internal sealed class DiscountEngine(
    IPromotionService promotionService,
    ILogger<DiscountEngine> logger) : IDiscountEngine
{
    public async Task<Result<DiscountResult>> CalculateAsync(
        IEnumerable<CartItem> cartItems,
        Guid? memberId,
        Guid shopId,
        CancellationToken ct = default)
    {
        var items = cartItems.ToList();
        var originalTotal = items.Sum(i => i.UnitPrice * i.Quantity);

        // Stub: no real tenant resolution in stub — use empty Guid as placeholder
        var activeResult = await promotionService.GetActiveForShopAsync(Guid.Empty, shopId, ct);
        if (!activeResult.IsSuccess)
            return Result<DiscountResult>.Failure(activeResult.Error!);

        var applicablePromotions = activeResult.Value!
            .Where(p => p.IsStackable)
            .ToList();

        // Stub: no formula evaluation — return 0 discount
        var discountAmount = 0m;

        logger.LogInformation(
            "Discount calculated: shopId={ShopId} originalTotal={Original} discount={Discount} promotionsConsidered={Count}",
            shopId, originalTotal, discountAmount, applicablePromotions.Count);

        var result = new DiscountResult(
            OriginalTotal: originalTotal,
            DiscountAmount: discountAmount,
            FinalTotal: originalTotal - discountAmount,
            AppliedPromotionIds: applicablePromotions.Select(p => p.Id.ToString()).ToList().AsReadOnly());

        return Result.Success(result);
    }
}

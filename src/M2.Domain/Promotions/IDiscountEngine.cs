using M2.SharedKernel;

namespace M2.Domain.Promotions;

/// <summary>
/// Calculates applicable discounts for a cart, respecting stackable flags (ADR-020).
/// </summary>
public interface IDiscountEngine
{
    Task<Result<DiscountResult>> CalculateAsync(
        IEnumerable<CartItem> cartItems,
        Guid? memberId,
        Guid shopId,
        CancellationToken ct = default);
}

namespace M2.Domain.Promotions;

/// <summary>Represents a single line item in a cart for discount calculation.</summary>
public record CartItem(string ProductId, int Quantity, decimal UnitPrice);

/// <summary>Result of a discount calculation pass over a cart.</summary>
public record DiscountResult(
    decimal OriginalTotal,
    decimal DiscountAmount,
    decimal FinalTotal,
    IReadOnlyList<string> AppliedPromotionIds);

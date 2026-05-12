namespace M2.Domain.Reporting;

/// <summary>Value object summarising daily sales for a shop.</summary>
public record DailySalesSummary(
    Guid ShopId,
    DateOnly Date,
    decimal TotalAmount,
    int TransactionCount,
    decimal DiscountTotal);

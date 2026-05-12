using M2.SharedKernel;

namespace M2.Domain.Reporting;

/// <summary>Value object reporting the performance of a single promotion.</summary>
public record PromotionPerformance(
    Guid PromotionId,
    BilingualText PromotionName,
    int CouponsIssued,
    int CouponsRedeemed,
    decimal TotalDiscountGiven);

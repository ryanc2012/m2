namespace M2.Domain.Reporting;

/// <summary>Aggregated sales summary for a shop on a given date.</summary>
public record SalesSummary(
    Guid TenantId,
    Guid ShopId,
    DateOnly Date,
    int TotalTransactions,
    decimal TotalRevenue,
    int TotalReturns,
    decimal TotalReturnValue);

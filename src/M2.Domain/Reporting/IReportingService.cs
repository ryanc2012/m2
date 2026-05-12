using M2.SharedKernel;

namespace M2.Domain.Reporting;

public interface IReportingService
{
    Task<Result<SalesSummary>> GetDailySalesSummaryAsync(
        Guid tenantId,
        Guid shopId,
        DateOnly date,
        CancellationToken ct = default);

    Task<Result<AttendanceSummary>> GetAttendanceSummaryAsync(
        Guid tenantId,
        Guid shopId,
        DateOnly date,
        CancellationToken ct = default);

    Task<Result<PromotionPerformance>> GetPromotionPerformanceAsync(
        Guid tenantId,
        Guid shopId,
        Guid promotionId,
        CancellationToken ct = default);
}

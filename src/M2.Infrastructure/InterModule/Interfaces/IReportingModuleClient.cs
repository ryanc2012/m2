using M2.Domain.Reporting;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IReportingModuleClient
{
    Task<Result<SalesSummary>> GetDailySalesSummaryAsync(Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default);
    Task<Result<AttendanceSummary>> GetAttendanceSummaryAsync(Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default);
    Task<Result<PromotionPerformance>> GetPromotionPerformanceAsync(Guid tenantId, Guid shopId, Guid promotionId, CancellationToken ct = default);
}

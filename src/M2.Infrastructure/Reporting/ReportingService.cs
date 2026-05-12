using M2.Domain.Reporting;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Reporting;

/// <summary>Stub ReportingService. Aggregation queries deferred to post-Sprint 4 EF implementation.</summary>
internal sealed class ReportingService(ILogger<ReportingService> logger) : IReportingService
{
    public Task<Result<SalesSummary>> GetDailySalesSummaryAsync(
        Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default)
    {
        logger.LogInformation("GetDailySalesSummary: tenant={TenantId} shop={ShopId} date={Date}",
            tenantId, shopId, date);
        var summary = new SalesSummary(tenantId, shopId, date, 0, 0m, 0, 0m);
        return Task.FromResult(Result.Success(summary));
    }

    public Task<Result<AttendanceSummary>> GetAttendanceSummaryAsync(
        Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default)
    {
        logger.LogInformation("GetAttendanceSummary: tenant={TenantId} shop={ShopId} date={Date}",
            tenantId, shopId, date);
        var summary = new AttendanceSummary(tenantId, shopId, date, 0, 0m);
        return Task.FromResult(Result.Success(summary));
    }

    public Task<Result<PromotionPerformance>> GetPromotionPerformanceAsync(
        Guid tenantId, Guid shopId, Guid promotionId, CancellationToken ct = default)
    {
        logger.LogInformation("GetPromotionPerformance: tenant={TenantId} promo={PromotionId}", tenantId, promotionId);
        var performance = new PromotionPerformance(promotionId, BilingualText.Empty, 0, 0, 0m);
        return Task.FromResult(Result.Success(performance));
    }
}

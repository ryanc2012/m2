namespace M2Portal.Services;

public record DashboardSummary(
    decimal TodayRevenue,
    int TodayTransactionCount,
    int TodayClockedIn,
    double TodayTotalHours,
    int ActivePromotionsCount,
    int PendingApprovalsCount);

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}

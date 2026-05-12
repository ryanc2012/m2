using System.Net.Http.Json;

namespace M2Portal.Services;

/// <summary>
/// Typed HTTP client wrapping M2PortalBff /api/reports/ endpoints.
/// Returns mock data when reporting APIs are unavailable (Sprint 4 stub).
/// </summary>
public class DashboardService(HttpClient http) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<DashboardSummary>(
                "/api/reports/dashboard-summary", ct)
                ?? MockSummary();
        }
        catch
        {
            return MockSummary();
        }
    }

    private static DashboardSummary MockSummary() => new(
        TodayRevenue: 4_820.50m,
        TodayTransactionCount: 37,
        TodayClockedIn: 8,
        TodayTotalHours: 52.5,
        ActivePromotionsCount: 3,
        PendingApprovalsCount: 2);
}

using System.Net.Http.Json;

namespace M2Portal.Services;

public record SalesSummaryRow(DateOnly Date, int TransactionCount, decimal Revenue);

public record AttendanceSummaryRow(
    string StaffId,
    string StaffName,
    DateOnly Date,
    TimeOnly? ClockIn,
    TimeOnly? ClockOut,
    decimal HoursWorked);

public class ReportingService(HttpClient http)
{
    public async Task<IReadOnlyList<SalesSummaryRow>> GetSalesSummaryAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        return await http.GetFromJsonAsync<List<SalesSummaryRow>>(
            $"/api/v1/reporting/sales{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<AttendanceSummaryRow>> GetAttendanceSummaryAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        return await http.GetFromJsonAsync<List<AttendanceSummaryRow>>(
            $"/api/v1/reporting/attendance{query}", ct) ?? [];
    }
}

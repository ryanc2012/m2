namespace M2.Domain.Reporting;

/// <summary>Aggregated attendance summary for a shop on a given date.</summary>
public record AttendanceSummary(
    Guid TenantId,
    Guid ShopId,
    DateOnly Date,
    int TotalClockedIn,
    decimal TotalHours);

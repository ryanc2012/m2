namespace M2.Domain.Reporting;

/// <summary>Value object summarising staff attendance for a shop on a given day.</summary>
public record AttendanceSummaryReport(
    Guid ShopId,
    DateOnly Date,
    int TotalEmployees,
    int PresentCount,
    double TotalHours);

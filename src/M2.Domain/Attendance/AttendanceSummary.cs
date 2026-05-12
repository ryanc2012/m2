namespace M2.Domain.Attendance;

/// <summary>
/// Value object summarising an employee's attendance for a given day.
/// </summary>
public record AttendanceSummary(
    string EmployeeId,
    DateOnly Date,
    double TotalHours,
    bool IsComplete);

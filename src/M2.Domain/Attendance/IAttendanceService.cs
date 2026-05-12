using M2.SharedKernel;

namespace M2.Domain.Attendance;

public interface IAttendanceService
{
    Task<Result<AttendanceRecord>> ClockInAsync(
        Guid tenantId,
        Guid shopId,
        string employeeId,
        AttendanceSource source = AttendanceSource.Manual,
        string? notes = null,
        CancellationToken ct = default);

    Task<Result<AttendanceRecord>> ClockOutAsync(
        Guid tenantId,
        string employeeId,
        string? notes = null,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<AttendanceRecord>>> GetRecordsForEmployeeAsync(
        Guid tenantId,
        string employeeId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    Task<Result<AttendanceSummary>> GetDailySummaryAsync(
        Guid tenantId,
        string employeeId,
        DateOnly date,
        CancellationToken ct = default);
}

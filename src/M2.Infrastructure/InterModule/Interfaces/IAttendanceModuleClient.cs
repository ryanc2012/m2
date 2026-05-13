using M2.Domain.Attendance;
using M2.Domain.Attendance.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IAttendanceModuleClient
{
    Task<Result<AttendanceRecordDto>> ClockInAsync(ClockInPayload payload, CancellationToken ct = default);
    Task<Result<AttendanceRecordDto>> ClockOutAsync(ClockOutPayload payload, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttendanceRecordDto>>> GetRecordsForEmployeeAsync(
        Guid tenantId,
        string employeeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default);
    Task<Result<AttendanceSummary>> GetDailySummaryAsync(
        Guid tenantId,
        string employeeId,
        DateOnly date,
        CancellationToken ct = default);
}

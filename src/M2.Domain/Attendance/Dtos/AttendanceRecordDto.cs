namespace M2.Domain.Attendance.Dtos;

public record AttendanceRecordDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    string EmployeeId,
    DateTimeOffset ClockInAt,
    DateTimeOffset? ClockOutAt,
    string Source,
    string? Notes,
    bool IsComplete);

public record ClockInPayload(
    Guid TenantId,
    Guid ShopId,
    string EmployeeId,
    string Source,
    string? Notes);

public record ClockOutPayload(
    Guid TenantId,
    string EmployeeId,
    string? Notes);

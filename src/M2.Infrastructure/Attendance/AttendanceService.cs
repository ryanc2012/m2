using M2.Domain.Attendance;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Attendance;

/// <summary>In-memory stub AttendanceService. EF wiring deferred to Sprint 4.</summary>
internal sealed class AttendanceService(ILogger<AttendanceService> logger) : IAttendanceService
{
    private static readonly Dictionary<Guid, AttendanceRecord> _records = [];

    public Task<Result<AttendanceRecord>> ClockInAsync(
        Guid tenantId, Guid shopId, string employeeId,
        AttendanceSource source = AttendanceSource.Manual,
        string? notes = null, CancellationToken ct = default)
    {
        var existing = _records.Values
            .FirstOrDefault(r => r.TenantId == tenantId && r.EmployeeId == employeeId && !r.IsComplete);

        if (existing is not null)
            return Task.FromResult(Result.Failure<AttendanceRecord>(
                $"Employee {employeeId} is already clocked in (record {existing.Id})."));

        var record = new AttendanceRecord(tenantId, shopId, employeeId, source, notes);
        _records[record.Id] = record;

        logger.LogInformation(
            "Clock-in: employee={EmployeeId} shop={ShopId} at={ClockIn}",
            employeeId, shopId, record.ClockInAt);

        return Task.FromResult(Result.Success(record));
    }

    public Task<Result<AttendanceRecord>> ClockOutAsync(
        Guid tenantId, string employeeId, string? notes = null, CancellationToken ct = default)
    {
        var record = _records.Values
            .FirstOrDefault(r => r.TenantId == tenantId && r.EmployeeId == employeeId && !r.IsComplete);

        if (record is null)
            return Task.FromResult(Result.Failure<AttendanceRecord>(
                $"No active clock-in found for employee {employeeId}."));

        record.ClockOut(notes);

        logger.LogInformation(
            "Clock-out: employee={EmployeeId} at={ClockOut} totalHours={Hours}",
            employeeId, record.ClockOutAt,
            (record.ClockOutAt!.Value - record.ClockInAt).TotalHours);

        return Task.FromResult(Result.Success(record));
    }

    public Task<Result<IReadOnlyList<AttendanceRecord>>> GetRecordsForEmployeeAsync(
        Guid tenantId, string employeeId,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = _records.Values
            .Where(r => r.TenantId == tenantId && r.EmployeeId == employeeId);

        if (from.HasValue) query = query.Where(r => r.ClockInAt >= from.Value);
        if (to.HasValue)   query = query.Where(r => r.ClockInAt <= to.Value);

        var result = query.OrderByDescending(r => r.ClockInAt).ToList() as IReadOnlyList<AttendanceRecord>;
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<AttendanceSummary>> GetDailySummaryAsync(
        Guid tenantId, string employeeId, DateOnly date, CancellationToken ct = default)
    {
        var dayRecords = _records.Values
            .Where(r => r.TenantId == tenantId
                     && r.EmployeeId == employeeId
                     && DateOnly.FromDateTime(r.ClockInAt.Date) == date)
            .ToList();

        var totalHours = dayRecords
            .Where(r => r.IsComplete)
            .Sum(r => (r.ClockOutAt!.Value - r.ClockInAt).TotalHours);

        var isComplete = dayRecords.All(r => r.IsComplete) && dayRecords.Count > 0;

        var summary = new AttendanceSummary(employeeId, date, totalHours, isComplete);
        return Task.FromResult(Result.Success(summary));
    }
}

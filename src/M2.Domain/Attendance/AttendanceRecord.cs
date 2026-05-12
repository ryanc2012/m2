using M2.SharedKernel;

namespace M2.Domain.Attendance;

public class AttendanceRecord : BaseEntity
{
    /// <summary>SAP employee code.</summary>
    public string EmployeeId { get; protected set; } = string.Empty;
    public DateTimeOffset ClockInAt { get; protected set; }
    /// <summary>Null while employee is still clocked in.</summary>
    public DateTimeOffset? ClockOutAt { get; protected set; }
    public AttendanceSource Source { get; protected set; }
    public string? Notes { get; protected set; }

    protected AttendanceRecord() { }

    public AttendanceRecord(
        Guid tenantId,
        Guid shopId,
        string employeeId,
        AttendanceSource source,
        string? notes = null)
    {
        TenantId = tenantId;
        ShopId = shopId;
        EmployeeId = employeeId;
        ClockInAt = DateTimeOffset.UtcNow;
        Source = source;
        Notes = notes;
    }

    public void ClockOut(string? notes = null)
    {
        ClockOutAt = DateTimeOffset.UtcNow;
        if (notes is not null) Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsComplete => ClockOutAt.HasValue;
}

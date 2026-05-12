using M2.Domain.Attendance;

namespace M2.MekaPosBff.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/attendance").WithTags("Attendance");

        group.MapPost("/clock-in", async (
            ClockInRequest req,
            IAttendanceService svc) =>
        {
            var result = await svc.ClockInAsync(req.TenantId, req.ShopId, req.EmployeeId, req.Source, req.Notes);
            return result.IsSuccess
                ? Results.Created($"/attendance/records/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/clock-out", async (
            ClockOutRequest req,
            IAttendanceService svc) =>
        {
            var result = await svc.ClockOutAsync(req.TenantId, req.EmployeeId, req.Notes);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/records", async (
            Guid tenantId,
            string employeeId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IAttendanceService svc) =>
        {
            var result = await svc.GetRecordsForEmployeeAsync(tenantId, employeeId, from, to);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/summary", async (
            Guid tenantId,
            string employeeId,
            DateOnly date,
            IAttendanceService svc) =>
        {
            var result = await svc.GetDailySummaryAsync(tenantId, employeeId, date);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record ClockInRequest(
    Guid TenantId,
    Guid ShopId,
    string EmployeeId,
    AttendanceSource Source = AttendanceSource.Manual,
    string? Notes = null);

public record ClockOutRequest(
    Guid TenantId,
    string EmployeeId,
    string? Notes = null);

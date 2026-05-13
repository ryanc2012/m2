using M2.Domain.Attendance;
using M2.Domain.Attendance.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.MekaPosBff.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/attendance").WithTags("Attendance");

        group.MapPost("/clock-in", async (
            ClockInRequest req,
            IAttendanceModuleClient client) =>
        {
            var payload = new ClockInPayload(req.TenantId, req.ShopId, req.EmployeeId, req.Source.ToString(), req.Notes);
            var result = await client.ClockInAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/attendance/records/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/clock-out", async (
            ClockOutRequest req,
            IAttendanceModuleClient client) =>
        {
            var payload = new ClockOutPayload(req.TenantId, req.EmployeeId, req.Notes);
            var result = await client.ClockOutAsync(payload);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/records", async (
            Guid tenantId,
            string employeeId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IAttendanceModuleClient client) =>
        {
            var result = await client.GetRecordsForEmployeeAsync(tenantId, employeeId, from, to);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/summary", async (
            Guid tenantId,
            string employeeId,
            DateOnly date,
            IAttendanceModuleClient client) =>
        {
            var result = await client.GetDailySummaryAsync(tenantId, employeeId, date);
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

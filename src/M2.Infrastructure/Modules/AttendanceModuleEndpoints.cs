using M2.Domain.Attendance;
using M2.Domain.Attendance.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class AttendanceModuleEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/attendance")
            .RequireAuthorization()
            .WithTags("Modules.Attendance");

        group.MapPost("/clock-in", async (ClockInPayload payload, IAttendanceService svc) =>
        {
            if (!Enum.TryParse<AttendanceSource>(payload.Source, out var source))
                return Results.BadRequest("Invalid attendance source");
            var result = await svc.ClockInAsync(payload.TenantId, payload.ShopId, payload.EmployeeId, source, payload.Notes);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/clock-out", async (ClockOutPayload payload, IAttendanceService svc) =>
        {
            var result = await svc.ClockOutAsync(payload.TenantId, payload.EmployeeId, payload.Notes);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapGet("/records", async (
            Guid tenantId,
            string employeeId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IAttendanceService svc) =>
        {
            var result = await svc.GetRecordsForEmployeeAsync(tenantId, employeeId, from, to);
            if (!result.IsSuccess) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value!.Select(ToDto).ToList());
        });

        group.MapGet("/summary", async (
            Guid tenantId,
            string employeeId,
            DateOnly date,
            IAttendanceService svc) =>
        {
            var result = await svc.GetDailySummaryAsync(tenantId, employeeId, date);
            return result.IsSuccess ? Results.Ok(result.Value!) : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord r) => new(
        r.Id, r.TenantId, r.ShopId,
        r.EmployeeId, r.ClockInAt, r.ClockOutAt,
        r.Source.ToString(), r.Notes, r.IsComplete);
}

using M2.Domain.Attendance;

namespace M2.M2PortalBff.Endpoints;

public static class AttendanceAdminEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/attendance").WithTags("Attendance");

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

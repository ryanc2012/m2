using M2.Domain.Reporting;

namespace M2.M2PortalBff.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reporting");

        group.MapGet("/sales/daily", async (
            Guid tenantId,
            Guid shopId,
            DateOnly date,
            IReportingService svc) =>
        {
            var result = await svc.GetDailySalesSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/attendance/daily", async (
            Guid tenantId,
            Guid shopId,
            DateOnly date,
            IReportingService svc) =>
        {
            var result = await svc.GetAttendanceSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/promotions/{promotionId:guid}/performance", async (
            Guid tenantId,
            Guid shopId,
            Guid promotionId,
            IReportingService svc) =>
        {
            var result = await svc.GetPromotionPerformanceAsync(tenantId, shopId, promotionId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return app;
    }
}

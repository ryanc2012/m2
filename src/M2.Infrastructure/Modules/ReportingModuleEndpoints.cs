using M2.Domain.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class ReportingModuleEndpoints
{
    public static IEndpointRouteBuilder MapReportingModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/reporting")
            .RequireAuthorization()
            .WithTags("Modules.Reporting");

        group.MapGet("/sales/daily", async (Guid tenantId, Guid shopId, DateOnly date, IReportingService svc) =>
        {
            var result = await svc.GetDailySalesSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value!) : Results.BadRequest(result.Error);
        });

        group.MapGet("/attendance/daily", async (Guid tenantId, Guid shopId, DateOnly date, IReportingService svc) =>
        {
            var result = await svc.GetAttendanceSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value!) : Results.BadRequest(result.Error);
        });

        group.MapGet("/promotions/{promotionId:guid}/performance", async (
            Guid tenantId,
            Guid shopId,
            Guid promotionId,
            IReportingService svc) =>
        {
            var result = await svc.GetPromotionPerformanceAsync(tenantId, shopId, promotionId);
            return result.IsSuccess ? Results.Ok(result.Value!) : Results.NotFound(result.Error);
        });

        return app;
    }
}

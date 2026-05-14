using M2.Domain.Authorization;
using M2.Infrastructure.InterModule.Interfaces;
using System.Security.Claims;

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
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IReportingModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_REPORTING_VIEW") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await client.GetDailySalesSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/attendance/daily", async (
            Guid tenantId,
            Guid shopId,
            DateOnly date,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IReportingModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_REPORTING_VIEW") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await client.GetAttendanceSummaryAsync(tenantId, shopId, date);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/promotions/{promotionId:guid}/performance", async (
            Guid tenantId,
            Guid shopId,
            Guid promotionId,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IReportingModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_REPORTING_VIEW") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await client.GetPromotionPerformanceAsync(tenantId, shopId, promotionId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return app;
    }
}

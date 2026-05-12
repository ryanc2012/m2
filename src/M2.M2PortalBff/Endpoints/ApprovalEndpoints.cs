using M2.Domain.Approvals;
using M2.SharedKernel;

namespace M2.M2PortalBff.Endpoints;

public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/approvals").WithTags("Approvals");

        group.MapPost("/requests", async (
            CreateApprovalRequestRequest req,
            IApprovalService svc) =>
        {
            var result = await svc.CreateRequestAsync(req.TenantId, req.ShopId, req.EntityType, req.EntityId);
            return result.IsSuccess
                ? Results.Created($"/approvals/requests/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/requests/{id:guid}", async (
            Guid id,
            IApprovalService svc) =>
        {
            var result = await svc.GetRequestAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/pending", async (
            string approverId,
            IApprovalService svc) =>
        {
            var result = await svc.GetPendingRequestsForApproverAsync(approverId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/requests/{id:guid}/approve", async (
            Guid id,
            ApproveRejectRequest req,
            IApprovalService svc) =>
        {
            var result = await svc.ApproveStepAsync(id, req.ApproverId, req.Comment);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/requests/{id:guid}/reject", async (
            Guid id,
            ApproveRejectRequest req,
            IApprovalService svc) =>
        {
            var result = await svc.RejectStepAsync(id, req.ApproverId, req.Comment);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record CreateApprovalRequestRequest(
    Guid TenantId,
    Guid ShopId,
    string EntityType,
    Guid EntityId);

public record ApproveRejectRequest(string ApproverId, string? Comment);

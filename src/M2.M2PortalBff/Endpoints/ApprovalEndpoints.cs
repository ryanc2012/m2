using M2.Domain.Approvals.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.M2PortalBff.Endpoints;

public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/approvals").WithTags("Approvals");

        group.MapPost("/requests", async (
            CreateApprovalRequestRequest req,
            IApprovalsModuleClient client) =>
        {
            var payload = new CreateApprovalPayload(req.TenantId, req.ShopId, req.EntityType, req.EntityId);
            var result = await client.CreateRequestAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/approvals/requests/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/requests/{id:guid}", async (
            Guid id,
            IApprovalsModuleClient client) =>
        {
            var result = await client.GetRequestAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/pending", async (
            string approverId,
            IApprovalsModuleClient client) =>
        {
            var result = await client.GetPendingRequestsForApproverAsync(approverId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/requests/{id:guid}/approve", async (
            Guid id,
            ApproveRejectRequest req,
            IApprovalsModuleClient client) =>
        {
            var payload = new ApproveRejectPayload(req.ApproverId, req.Comment);
            var result = await client.ApproveStepAsync(id, payload);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/requests/{id:guid}/reject", async (
            Guid id,
            ApproveRejectRequest req,
            IApprovalsModuleClient client) =>
        {
            var payload = new ApproveRejectPayload(req.ApproverId, req.Comment);
            var result = await client.RejectStepAsync(id, payload);
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

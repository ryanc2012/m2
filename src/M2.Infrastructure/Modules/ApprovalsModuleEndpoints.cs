using M2.Domain.Approvals;
using M2.Domain.Approvals.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class ApprovalsModuleEndpoints
{
    public static IEndpointRouteBuilder MapApprovalsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/approvals")
            .RequireAuthorization()
            .WithTags("Modules.Approvals");

        group.MapPost("/requests", async (CreateApprovalPayload payload, IApprovalService svc) =>
        {
            var result = await svc.CreateRequestAsync(payload.TenantId, payload.ShopId, payload.EntityType, payload.EntityId);
            return result.IsSuccess
                ? Results.Ok(ToDto(result.Value!))
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/requests/{id:guid}", async (Guid id, IApprovalService svc) =>
        {
            var result = await svc.GetRequestAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapGet("/pending", async (string approverId, IApprovalService svc) =>
        {
            var result = await svc.GetPendingRequestsForApproverAsync(approverId);
            if (!result.IsSuccess) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value!.Select(ToDto).ToList());
        });

        group.MapPost("/requests/{id:guid}/approve", async (Guid id, ApproveRejectPayload payload, IApprovalService svc) =>
        {
            var result = await svc.ApproveStepAsync(id, payload.ApproverId, payload.Comment);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/requests/{id:guid}/reject", async (Guid id, ApproveRejectPayload payload, IApprovalService svc) =>
        {
            var result = await svc.RejectStepAsync(id, payload.ApproverId, payload.Comment);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static ApprovalRequestDto ToDto(ApprovalRequest r) => new(
        r.Id, r.TenantId, r.ShopId, r.EntityType, r.EntityId, r.Status.ToString(), r.CurrentStep);
}

using M2.Domain.Approvals;
using M2.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Approvals;

/// <summary>
/// EF Core–backed approval service (S4.2). Replaces in-memory stub.
/// Persists all approval requests and steps to the database via M2DbContext.
/// </summary>
internal sealed class ApprovalService(
    M2DbContext context,
    IApprovalPolicyService policyService,
    IPublisher publisher,
    ILogger<ApprovalService> logger) : IApprovalService
{
    public async Task<Result<ApprovalRequest>> CreateRequestAsync(
        Guid tenantId, Guid shopId, string entityType, Guid entityId)
    {
        var request = new ApprovalRequest(tenantId, shopId, entityType, entityId);
        context.ApprovalRequests.Add(request);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "ApprovalRequest created: {Id} for {EntityType}/{EntityId} tenant={TenantId}",
            request.Id, entityType, entityId, tenantId);

        return Result.Success(request);
    }

    public async Task<Result<ApprovalRequest>> ApproveStepAsync(
        Guid requestId, string approverId, string? comment, CancellationToken ct = default)
    {
        var request = await context.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
            return Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found.");

        if (request.Status != ApprovalStatus.Pending)
            return Result.Failure<ApprovalRequest>("Request is not in Pending state.");

        var policy = await policyService.GetPolicyAsync(request.TenantId, request.EntityType, ct);
        var maxLevels = policy.IsSuccess && policy.Value is not null ? policy.Value.MaxLevels : 2;
        var mode = policy.IsSuccess && policy.Value is not null
            ? policy.Value.Mode
            : ApprovalMode.StepByStepPosition;

        var step = new ApprovalStep(
            request.TenantId, request.ShopId, requestId,
            request.CurrentStep, approverId,
            mode == ApprovalMode.SapHcmHierarchy ? ApproverType.SapHcm : ApproverType.SapPosition);

        step.Approve(comment);
        context.ApprovalSteps.Add(step);

        if (request.CurrentStep >= maxLevels)
        {
            request.SetStatus(ApprovalStatus.Approved);
            logger.LogInformation("ApprovalRequest {Id} fully approved after {Levels} levels.", requestId, maxLevels);
        }
        else
        {
            request.AdvanceStep();
            logger.LogInformation("ApprovalRequest {Id} advanced to step {Step}/{Max}.",
                requestId, request.CurrentStep, maxLevels);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success(request);
    }

    public async Task<Result<ApprovalRequest>> RejectStepAsync(
        Guid requestId, string approverId, string? comment, CancellationToken ct = default)
    {
        var request = await context.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
            return Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found.");

        if (request.Status != ApprovalStatus.Pending)
            return Result.Failure<ApprovalRequest>("Request is not in Pending state.");

        var step = new ApprovalStep(
            request.TenantId, request.ShopId, requestId,
            request.CurrentStep, approverId, ApproverType.SapPosition);
        step.Reject(comment);
        context.ApprovalSteps.Add(step);

        request.SetStatus(ApprovalStatus.Rejected);
        logger.LogInformation("ApprovalRequest {Id} rejected by {ApproverId}.", requestId, approverId);

        await context.SaveChangesAsync(ct);
        return Result.Success(request);
    }

    public async Task<Result<ApprovalRequest>> GetRequestAsync(Guid requestId, CancellationToken ct = default)
    {
        var request = await context.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        return request is not null
            ? Result.Success(request)
            : Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found.");
    }

    public async Task<Result<IReadOnlyList<ApprovalRequest>>> GetPendingRequestsForApproverAsync(
        string approverId, CancellationToken ct = default)
    {
        var requests = await context.ApprovalRequests
            .Include(r => r.Steps)
            .Where(r => r.Status == ApprovalStatus.Pending &&
                        (!r.Steps.Any() ||
                         r.Steps.Any(s => s.ApproverId == approverId && s.Status == ApprovalStatus.Pending)))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<ApprovalRequest>>(requests);
    }

    public async Task<Result<ApprovalRequest>> EscalateAsync(
        Guid requestId, string escalatedBy, string reason, CancellationToken ct = default)
    {
        var request = await context.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
            return Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found.");

        if (request.Status != ApprovalStatus.Pending)
            return Result.Failure<ApprovalRequest>("Only Pending requests can be escalated.");

        // Record escalation as a step
        var step = new ApprovalStep(
            request.TenantId, request.ShopId, requestId,
            request.CurrentStep, escalatedBy, ApproverType.SapPosition);
        step.Approve($"[ESCALATED] {reason}");
        context.ApprovalSteps.Add(step);

        request.SetStatus(ApprovalStatus.Escalated);
        await context.SaveChangesAsync(ct);

        await publisher.Publish(new ApprovalRequestEscalatedEvent(
            RequestId: requestId,
            EntityType: request.EntityType,
            EntityId: request.EntityId,
            EscalatedBy: escalatedBy,
            Reason: reason,
            EscalatedAt: DateTimeOffset.UtcNow), ct);

        logger.LogInformation("ApprovalRequest {Id} escalated by {EscalatedBy}. Reason: {Reason}",
            requestId, escalatedBy, reason);

        return Result.Success(request);
    }
}


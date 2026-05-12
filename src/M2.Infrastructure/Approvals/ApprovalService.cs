using M2.Domain.Approvals;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Approvals;

/// <summary>
/// In-memory stub implementing IApprovalService.
/// Respects ApprovalPolicy.Mode (ADR-014) and MaxLevels (ADR-021).
/// Replace in-memory store with EF DbContext queries in Sprint 3+.
/// </summary>
internal sealed class ApprovalService(
    IApprovalPolicyService policyService,
    ILogger<ApprovalService> logger) : IApprovalService
{
    // In-memory store — stub until EF migrations are in place
    private static readonly Dictionary<Guid, ApprovalRequest> _requests = [];
    private static readonly Dictionary<Guid, List<ApprovalStep>> _steps = [];

    public Task<Result<ApprovalRequest>> CreateRequestAsync(
        Guid tenantId, Guid shopId, string entityType, Guid entityId)
    {
        var request = new ApprovalRequest(tenantId, shopId, entityType, entityId);
        _requests[request.Id] = request;
        _steps[request.Id] = [];

        logger.LogInformation(
            "ApprovalRequest created: {Id} for {EntityType}/{EntityId} tenant={TenantId}",
            request.Id, entityType, entityId, tenantId);

        return Task.FromResult(Result.Success(request));
    }

    public async Task<Result<ApprovalRequest>> ApproveStepAsync(
        Guid requestId, string approverId, string? comment, CancellationToken ct = default)
    {
        if (!_requests.TryGetValue(requestId, out var request))
            return Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found.");

        if (request.Status != ApprovalStatus.Pending)
            return Result.Failure<ApprovalRequest>($"Request is not in Pending state.");

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
        _steps[requestId].Add(step);

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

        return Result.Success(request);
    }

    public Task<Result<ApprovalRequest>> RejectStepAsync(
        Guid requestId, string approverId, string? comment, CancellationToken ct = default)
    {
        if (!_requests.TryGetValue(requestId, out var request))
            return Task.FromResult(Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found."));

        if (request.Status != ApprovalStatus.Pending)
            return Task.FromResult(Result.Failure<ApprovalRequest>("Request is not in Pending state."));

        var step = new ApprovalStep(
            request.TenantId, request.ShopId, requestId,
            request.CurrentStep, approverId, ApproverType.SapPosition);
        step.Reject(comment);
        _steps[requestId].Add(step);

        request.SetStatus(ApprovalStatus.Rejected);
        logger.LogInformation("ApprovalRequest {Id} rejected by {ApproverId}.", requestId, approverId);

        return Task.FromResult(Result.Success(request));
    }

    public Task<Result<ApprovalRequest>> GetRequestAsync(Guid requestId, CancellationToken ct = default)
    {
        return _requests.TryGetValue(requestId, out var request)
            ? Task.FromResult(Result.Success(request))
            : Task.FromResult(Result.Failure<ApprovalRequest>($"ApprovalRequest {requestId} not found."));
    }

    public Task<Result<IReadOnlyList<ApprovalRequest>>> GetPendingRequestsForApproverAsync(
        string approverId, CancellationToken ct = default)
    {
        var pending = _requests.Values
            .Where(r => r.Status == ApprovalStatus.Pending &&
                        _steps.TryGetValue(r.Id, out var steps) &&
                        steps.Any(s => s.ApproverId == approverId && s.Status == ApprovalStatus.Pending))
            .ToList();

        // Also include requests at current step with no steps recorded yet (awaiting first action)
        var unassigned = _requests.Values
            .Where(r => r.Status == ApprovalStatus.Pending &&
                        (!_steps.ContainsKey(r.Id) || _steps[r.Id].Count == 0))
            .ToList();

        var result = pending.Union(unassigned).ToList() as IReadOnlyList<ApprovalRequest>;
        return Task.FromResult(Result.Success(result));
    }
}

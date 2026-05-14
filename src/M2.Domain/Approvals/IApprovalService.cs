using M2.SharedKernel;

namespace M2.Domain.Approvals;

public interface IApprovalService
{
    Task<Result<ApprovalRequest>> CreateRequestAsync(
        Guid tenantId,
        Guid shopId,
        string entityType,
        Guid entityId);

    Task<Result<ApprovalRequest>> ApproveStepAsync(
        Guid requestId,
        string approverId,
        string? comment,
        CancellationToken ct = default);

    Task<Result<ApprovalRequest>> RejectStepAsync(
        Guid requestId,
        string approverId,
        string? comment,
        CancellationToken ct = default);

    Task<Result<ApprovalRequest>> GetRequestAsync(Guid requestId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ApprovalRequest>>> GetPendingRequestsForApproverAsync(
        string approverId,
        CancellationToken ct = default);

    Task<Result<ApprovalRequest>> EscalateAsync(
        Guid requestId,
        string escalatedBy,
        string reason,
        CancellationToken ct = default);
}


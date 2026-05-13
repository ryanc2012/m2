using M2.Domain.Approvals.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IApprovalsModuleClient
{
    Task<Result<ApprovalRequestDto>> CreateRequestAsync(CreateApprovalPayload payload, CancellationToken ct = default);
    Task<Result<ApprovalRequestDto>> GetRequestAsync(Guid requestId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApprovalRequestDto>>> GetPendingRequestsForApproverAsync(string approverId, CancellationToken ct = default);
    Task<Result<ApprovalRequestDto>> ApproveStepAsync(Guid requestId, ApproveRejectPayload payload, CancellationToken ct = default);
    Task<Result<ApprovalRequestDto>> RejectStepAsync(Guid requestId, ApproveRejectPayload payload, CancellationToken ct = default);
}

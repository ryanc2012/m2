using M2.SharedKernel;

namespace M2.Domain.Approvals;

public interface IApprovalPolicyService
{
    Task<Result<ApprovalPolicy?>> GetPolicyAsync(
        Guid tenantId,
        string entityType,
        CancellationToken ct = default);

    Task<Result<ApprovalPolicy>> SetPolicyAsync(
        Guid tenantId,
        Guid shopId,
        string entityType,
        ApprovalMode mode,
        int maxLevels,
        CancellationToken ct = default);
}

using M2.Domain.Approvals;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Approvals;

internal sealed class ApprovalPolicyService(ILogger<ApprovalPolicyService> logger) : IApprovalPolicyService
{
    private static readonly Dictionary<string, ApprovalPolicy> _policies = [];

    public Task<Result<ApprovalPolicy?>> GetPolicyAsync(
        Guid tenantId, string entityType, CancellationToken ct = default)
    {
        var key = $"{tenantId}:{entityType}";
        _policies.TryGetValue(key, out var policy);
        return Task.FromResult(Result.Success(policy));
    }

    public Task<Result<ApprovalPolicy>> SetPolicyAsync(
        Guid tenantId, Guid shopId, string entityType,
        ApprovalMode mode, int maxLevels, CancellationToken ct = default)
    {
        var key = $"{tenantId}:{entityType}";

        if (_policies.TryGetValue(key, out var existing))
        {
            existing.Update(mode, maxLevels);
            logger.LogInformation(
                "ApprovalPolicy updated for tenant={TenantId} entityType={EntityType} mode={Mode} levels={Levels}",
                tenantId, entityType, mode, maxLevels);
            return Task.FromResult(Result.Success(existing));
        }

        var policy = new ApprovalPolicy(tenantId, shopId, entityType, mode, maxLevels);
        _policies[key] = policy;
        logger.LogInformation(
            "ApprovalPolicy created for tenant={TenantId} entityType={EntityType} mode={Mode} levels={Levels}",
            tenantId, entityType, mode, maxLevels);
        return Task.FromResult(Result.Success(policy));
    }
}

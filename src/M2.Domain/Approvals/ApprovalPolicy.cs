using M2.SharedKernel;

namespace M2.Domain.Approvals;

/// <summary>
/// Per-tenant, per-entity-type approval policy.
/// ADR-014: dual-mode (SapHcmHierarchy / StepByStepPosition).
/// ADR-021: MaxLevels drives configurable N-level chain depth.
/// </summary>
public class ApprovalPolicy : BaseEntity
{
    public string EntityType { get; protected set; } = string.Empty;
    public ApprovalMode Mode { get; protected set; } = ApprovalMode.StepByStepPosition;
    /// <summary>Maximum number of approval levels (N-level chain, ADR-021).</summary>
    public int MaxLevels { get; protected set; } = 2;

    protected ApprovalPolicy() { }

    public ApprovalPolicy(
        Guid tenantId,
        Guid shopId,
        string entityType,
        ApprovalMode mode,
        int maxLevels)
    {
        TenantId = tenantId;
        ShopId = shopId;
        EntityType = entityType;
        Mode = mode;
        MaxLevels = maxLevels;
    }

    public void Update(ApprovalMode mode, int maxLevels)
    {
        Mode = mode;
        MaxLevels = maxLevels;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

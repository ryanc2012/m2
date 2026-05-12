using M2.SharedKernel;

namespace M2.Domain.Approvals;

/// <summary>
/// Document-agnostic approval request (BE-REC-001 R3).
/// Any entity type plugs in via EntityType + EntityId contract.
/// </summary>
public class ApprovalRequest : BaseEntity
{
    public string EntityType { get; protected set; } = string.Empty;
    public Guid EntityId { get; protected set; }
    public ApprovalStatus Status { get; protected set; } = ApprovalStatus.Pending;
    public int CurrentStep { get; protected set; } = 1;
    public ICollection<ApprovalStep> Steps { get; protected set; } = new List<ApprovalStep>();

    protected ApprovalRequest() { }

    public ApprovalRequest(Guid tenantId, Guid shopId, string entityType, Guid entityId)
    {
        TenantId = tenantId;
        ShopId = shopId;
        EntityType = entityType;
        EntityId = entityId;
        Status = ApprovalStatus.Pending;
    }

    public void SetStatus(ApprovalStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AdvanceStep()
    {
        CurrentStep++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

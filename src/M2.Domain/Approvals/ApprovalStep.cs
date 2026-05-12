using M2.SharedKernel;

namespace M2.Domain.Approvals;

public class ApprovalStep : BaseEntity
{
    public Guid RequestId { get; protected set; }
    public int StepNumber { get; protected set; }
    /// <summary>SAP position code or HCM hierarchy node identifier.</summary>
    public string ApproverId { get; protected set; } = string.Empty;
    public ApproverType ApproverType { get; protected set; }
    public ApprovalStatus Status { get; protected set; } = ApprovalStatus.Pending;
    public string? Comment { get; protected set; }
    public DateTimeOffset? ActedAt { get; protected set; }

    protected ApprovalStep() { }

    public ApprovalStep(
        Guid tenantId,
        Guid shopId,
        Guid requestId,
        int stepNumber,
        string approverId,
        ApproverType approverType)
    {
        TenantId = tenantId;
        ShopId = shopId;
        RequestId = requestId;
        StepNumber = stepNumber;
        ApproverId = approverId;
        ApproverType = approverType;
        Status = ApprovalStatus.Pending;
    }

    public void Approve(string? comment)
    {
        Status = ApprovalStatus.Approved;
        Comment = comment;
        ActedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? comment)
    {
        Status = ApprovalStatus.Rejected;
        Comment = comment;
        ActedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

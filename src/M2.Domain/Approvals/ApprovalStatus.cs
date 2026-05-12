namespace M2.Domain.Approvals;

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Escalated
}

public enum ApproverType
{
    SapPosition,
    SapHcm
}

/// <summary>
/// ADR-014: Both SAP HCM hierarchy and step-by-step position modes supported.
/// ADR-021: N-level configurable chain via MaxLevels.
/// </summary>
public enum ApprovalMode
{
    SapHcmHierarchy,
    StepByStepPosition
}

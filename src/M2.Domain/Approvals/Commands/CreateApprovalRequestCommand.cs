namespace M2.Domain.Approvals.Commands;

public record CreateApprovalRequestCommand(
    string DocumentType,
    Guid DocumentId,
    string RequestedBy,
    int TotalSteps,
    bool UseHcmHierarchy = false,
    int MaxHcmLevels = 0);

namespace M2.Domain.Approvals.Dtos;

public record ApprovalRequestDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    string EntityType,
    Guid EntityId,
    string Status,
    int CurrentStep);

public record CreateApprovalPayload(
    Guid TenantId,
    Guid ShopId,
    string EntityType,
    Guid EntityId);

public record ApproveRejectPayload(
    string ApproverId,
    string? Comment);

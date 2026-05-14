using MediatR;

namespace M2.Infrastructure.Approvals;

/// <summary>
/// MediatR notification emitted when an approval request is escalated (S4.2).
/// Handlers can send push notifications, update SAP, etc.
/// </summary>
public record ApprovalRequestEscalatedEvent(
    Guid RequestId,
    string EntityType,
    Guid EntityId,
    string EscalatedBy,
    string Reason,
    DateTimeOffset EscalatedAt) : INotification;

namespace M2.Domain.Notifications;

public enum NotificationType
{
    PushPromotion,
    ApprovalRequest,
    ApprovalDecision,
    SystemAlert
}

public enum DevicePlatform
{
    iOS,
    Android
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

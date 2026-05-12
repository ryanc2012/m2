using M2.SharedKernel;

namespace M2.Domain.Notifications;

public class NotificationLog : BaseEntity
{
    public Guid TemplateId { get; protected set; }
    public string RecipientUserId { get; protected set; } = string.Empty;
    public DateTimeOffset SentAt { get; protected set; }
    public NotificationStatus Status { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected NotificationLog() { }

    public NotificationLog(
        Guid tenantId,
        Guid shopId,
        Guid templateId,
        string recipientUserId,
        NotificationStatus status,
        string? errorMessage = null)
    {
        TenantId = tenantId;
        ShopId = shopId;
        TemplateId = templateId;
        RecipientUserId = recipientUserId;
        SentAt = DateTimeOffset.UtcNow;
        Status = status;
        ErrorMessage = errorMessage;
    }
}

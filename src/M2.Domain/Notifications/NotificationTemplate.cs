using M2.SharedKernel;

namespace M2.Domain.Notifications;

public class NotificationTemplate : BaseEntity
{
    public NotificationType Type { get; protected set; }
    public BilingualText Title { get; protected set; } = BilingualText.Empty;
    public BilingualText Body { get; protected set; } = BilingualText.Empty;

    protected NotificationTemplate() { }

    public NotificationTemplate(
        Guid tenantId,
        Guid shopId,
        NotificationType type,
        BilingualText title,
        BilingualText body)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Type = type;
        Title = title;
        Body = body;
    }
}

namespace M2.Domain.Notifications.Dtos;

public record NotificationLogDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    Guid TemplateId,
    string RecipientUserId,
    DateTimeOffset SentAt,
    string Status,
    bool IsRead,
    string? ErrorMessage);

namespace M2.Domain.Notifications.Commands;

public record SendPushCommand(
    string UserId,
    string TitleEn,
    string BodyEn,
    Guid TenantId,
    Guid ShopId);

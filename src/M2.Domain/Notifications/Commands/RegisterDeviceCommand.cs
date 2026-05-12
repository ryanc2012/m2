namespace M2.Domain.Notifications.Commands;

public record RegisterDeviceCommand(
    string UserId,
    string Platform,
    string Token,
    Guid TenantId,
    Guid ShopId);

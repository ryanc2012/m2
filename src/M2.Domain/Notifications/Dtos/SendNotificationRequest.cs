namespace M2.Domain.Notifications.Dtos;

public record SendNotificationRequest(
    string UserId,
    Guid TemplateId,
    Dictionary<string, string> Parameters);

using M2.SharedKernel;

namespace M2.Domain.Notifications;

public interface INotificationHistoryService
{
    Task<Result<IReadOnlyList<NotificationLog>>> GetByMemberAsync(
        Guid tenantId,
        string memberId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<NotificationLog>>> GetByShopAsync(
        Guid tenantId,
        Guid shopId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<Result> MarkReadAsync(
        Guid notificationLogId,
        CancellationToken ct = default);
}

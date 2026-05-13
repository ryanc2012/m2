using M2.Domain.Notifications.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface INotificationsModuleClient
{
    Task<Result> SendAsync(SendNotificationRequest request, CancellationToken ct = default);

    Task<Result<PagedResult<NotificationLogDto>>> GetHistoryByMemberAsync(
        Guid tenantId,
        string memberId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result> MarkReadAsync(Guid notificationLogId, CancellationToken ct = default);
}

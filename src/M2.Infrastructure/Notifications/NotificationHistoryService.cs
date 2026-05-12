using M2.Domain.Notifications;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Notifications;

/// <summary>Stub NotificationHistoryService. EF query wiring deferred post-Sprint 4.</summary>
internal sealed class NotificationHistoryService(ILogger<NotificationHistoryService> logger)
    : INotificationHistoryService
{
    public Task<Result<IReadOnlyList<NotificationLog>>> GetByMemberAsync(
        Guid tenantId, string memberId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        logger.LogInformation("GetByMember: tenant={TenantId} member={MemberId}", tenantId, memberId);
        return Task.FromResult(Result.Success<IReadOnlyList<NotificationLog>>(Array.Empty<NotificationLog>()));
    }

    public Task<Result<IReadOnlyList<NotificationLog>>> GetByShopAsync(
        Guid tenantId, Guid shopId, DateTimeOffset fromDate, DateTimeOffset toDate,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        logger.LogInformation(
            "GetByShop: tenant={TenantId} shop={ShopId} from={From} to={To}",
            tenantId, shopId, fromDate, toDate);
        return Task.FromResult(Result.Success<IReadOnlyList<NotificationLog>>(Array.Empty<NotificationLog>()));
    }

    public Task<Result> MarkReadAsync(Guid notificationLogId, CancellationToken ct = default)
    {
        logger.LogInformation("MarkRead: notificationLogId={Id}", notificationLogId);
        return Task.FromResult(Result.Success());
    }
}

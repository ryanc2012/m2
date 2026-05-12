using M2.Domain.Notifications;

namespace M2.M2PortalBff.Endpoints;

public static class NotificationHistoryAdminEndpoints
{
    public static IEndpointRouteBuilder MapNotificationHistoryAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications/history").WithTags("NotificationHistory");

        group.MapGet("/shop", async (
            Guid tenantId,
            Guid shopId,
            DateTimeOffset fromDate,
            DateTimeOffset toDate,
            int page,
            int pageSize,
            INotificationHistoryService svc) =>
        {
            var result = await svc.GetByShopAsync(tenantId, shopId, fromDate, toDate, page, pageSize);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPatch("/{notificationLogId:guid}/read", async (
            Guid notificationLogId,
            INotificationHistoryService svc) =>
        {
            var result = await svc.MarkReadAsync(notificationLogId);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

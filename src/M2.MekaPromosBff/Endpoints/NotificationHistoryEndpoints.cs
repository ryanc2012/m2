using M2.Domain.Notifications;

namespace M2.MekaPromosBff.Endpoints;

public static class NotificationHistoryEndpoints
{
    public static IEndpointRouteBuilder MapNotificationHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications/history").WithTags("NotificationHistory");

        group.MapGet("/", async (
            Guid tenantId,
            string memberId,
            int page,
            int pageSize,
            INotificationHistoryService svc) =>
        {
            var result = await svc.GetByMemberAsync(tenantId, memberId, page, pageSize);
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

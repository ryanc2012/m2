using M2.Infrastructure.InterModule.Interfaces;

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
            INotificationsModuleClient client) =>
        {
            var result = await client.GetHistoryByMemberAsync(tenantId, memberId, page, pageSize);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPatch("/{notificationLogId:guid}/read", async (
            Guid notificationLogId,
            INotificationsModuleClient client) =>
        {
            var result = await client.MarkReadAsync(notificationLogId);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

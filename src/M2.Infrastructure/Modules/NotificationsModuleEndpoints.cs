using M2.Domain.Notifications;
using M2.Domain.Notifications.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class NotificationsModuleEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/notifications")
            .RequireAuthorization() // internal call secret validated by ApiKeyMiddleware
            .WithTags("Modules.Notifications");

        group.MapPost("/send", async (SendNotificationRequest req, INotificationService svc) =>
        {
            var result = await svc.SendPushAsync(req.UserId, req.TemplateId, req.Parameters);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        group.MapGet("/history/member/{memberId}", async (
            Guid tenantId,
            string memberId,
            int page,
            int pageSize,
            INotificationHistoryService svc) =>
        {
            var result = await svc.GetByMemberAsync(tenantId, memberId, page, pageSize);
            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            var paged = new PagedResult<NotificationLogDto>(
                result.Value!
                    .Select(log => new NotificationLogDto(
                        log.Id, log.TenantId, log.ShopId, log.TemplateId,
                        log.RecipientUserId, log.SentAt, log.Status.ToString(),
                        log.IsRead, log.ErrorMessage))
                    .ToList()
                    .AsReadOnly(),
                result.Value!.Count,
                page,
                pageSize);

            return Results.Ok(paged);
        });

        group.MapPatch("/history/{id}/read", async (Guid id, INotificationHistoryService svc) =>
        {
            var result = await svc.MarkReadAsync(id);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

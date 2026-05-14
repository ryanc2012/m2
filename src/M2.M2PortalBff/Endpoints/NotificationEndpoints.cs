using M2.Domain.Authorization;
using M2.Domain.Notifications;
using System.Security.Claims;

namespace M2.M2PortalBff.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notifications");

        group.MapPost("/devices", async (
            RegisterDeviceRequest req,
            INotificationService svc) =>
        {
            var result = await svc.RegisterDeviceAsync(
                req.TenantId, req.ShopId, req.UserId,
                req.Platform, req.FcmToken, req.ApnsToken);

            return result.IsSuccess
                ? Results.Created($"/notifications/devices/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapDelete("/devices/{id:guid}", async (
            Guid id,
            INotificationService svc) =>
        {
            var result = await svc.UnregisterDeviceAsync(id);
            return result.IsSuccess ? Results.NoContent() : Results.NotFound(result.Error);
        });

        group.MapPost("/send", async (
            SendNotificationRequest req,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            INotificationService svc) =>
        {
            if (await authz.CheckAsync(user, "M_NOTIFICATION_MANAGE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await svc.SendPushAsync(req.UserId, req.TemplateId, req.Parameters ?? []);
            return result.IsSuccess ? Results.Accepted() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record RegisterDeviceRequest(
    Guid TenantId,
    Guid ShopId,
    string UserId,
    DevicePlatform Platform,
    string? FcmToken,
    string? ApnsToken);

public record SendNotificationRequest(
    string UserId,
    Guid TemplateId,
    Dictionary<string, string>? Parameters);

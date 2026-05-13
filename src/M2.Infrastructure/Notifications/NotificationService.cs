using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using M2.Domain.Notifications;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Notifications;

/// <summary>
/// Real notification service: FCM push via FirebaseAdmin SDK (S4.6)
/// + SignalR real-time dispatch to connected Portal/BFF clients (S4.5).
/// </summary>
internal sealed class NotificationService(
    M2DbContext db,
    ISignalRNotificationDispatcher? signalR,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<Result> SendPushAsync(
        string userId, Guid templateId, Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("SendPushAsync called with null/empty userId — skipping dispatch");
            return Result.Success();
        }

        // S4.5 — push via SignalR to any connected web/BFF clients
        if (signalR is not null)
        {
            var hubPayload = new { TemplateId = templateId, Parameters = parameters };
            try
            {
                await signalR.SendToUserAsync(userId, hubPayload, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SignalR dispatch failed for userId={UserId}", userId);
            }
        }

        // S4.6 — FCM push to registered mobile devices
        var registrations = await db.DeviceRegistrations
            .Where(d => d.UserId == userId && d.FcmToken != null)
            .ToListAsync(ct);

        if (registrations.Count == 0)
        {
            logger.LogInformation(
                "No device registrations for userId={UserId} templateId={TemplateId}", userId, templateId);
            return Result.Success();
        }

        var messaging = GetMessagingOrNull();
        if (messaging is null)
        {
            logger.LogWarning(
                "Firebase not initialised — skipping FCM push for userId={UserId}", userId);
            return Result.Success();
        }

        var notification = new Notification { Title = templateId.ToString(), Body = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")) };
        var staleTokenIds = new List<Guid>();
        var sentCount = 0;

        foreach (var reg in registrations)
        {
            try
            {
                await messaging.SendAsync(new Message
                {
                    Token = reg.FcmToken,
                    Notification = notification,
                    Data = parameters
                }, ct);
                sentCount++;
            }
            catch (FirebaseMessagingException fex) when (fex.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                logger.LogInformation(
                    "FCM token unregistered for registrationId={Id} — scheduling removal", reg.Id);
                staleTokenIds.Add(reg.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "FCM send failed for registrationId={Id} userId={UserId}", reg.Id, userId);
            }
        }

        // Remove stale tokens from DB
        if (staleTokenIds.Count > 0)
        {
            var stale = await db.DeviceRegistrations
                .Where(d => staleTokenIds.Contains(d.Id))
                .ToListAsync(ct);
            db.DeviceRegistrations.RemoveRange(stale);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "FCM push: userId={UserId} sent={Sent}/{Total} templateId={TemplateId}",
            userId, sentCount, registrations.Count, templateId);

        return Result.Success();
    }

    public async Task<Result<DeviceRegistration>> RegisterDeviceAsync(
        Guid tenantId, Guid shopId, string userId,
        DevicePlatform platform, string? fcmToken, string? apnsToken,
        CancellationToken ct = default)
    {
        var registration = new DeviceRegistration(tenantId, shopId, userId, platform, fcmToken, apnsToken);
        db.DeviceRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Device registered: userId={UserId} platform={Platform} id={Id}",
            userId, platform, registration.Id);

        return Result.Success(registration);
    }

    public async Task<Result> UnregisterDeviceAsync(Guid registrationId, CancellationToken ct = default)
    {
        var reg = await db.DeviceRegistrations.FindAsync([registrationId], ct);
        if (reg is null)
        {
            logger.LogWarning("UnregisterDevice: registration {Id} not found.", registrationId);
            return Result.Failure($"Device registration {registrationId} not found.");
        }

        db.DeviceRegistrations.Remove(reg);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Device unregistered: {Id}", registrationId);
        return Result.Success();
    }

    private static FirebaseMessaging? GetMessagingOrNull()
    {
        try
        {
            return FirebaseMessaging.DefaultInstance;
        }
        catch
        {
            return null;
        }
    }
}

using M2.Domain.Notifications;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Notifications;

/// <summary>
/// Stub notification service. Logs dispatch intent to Serilog — no real FCM/APNs calls yet.
/// Firebase Admin SDK wiring deferred to Sprint 3.
/// </summary>
internal sealed class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    private static readonly Dictionary<Guid, DeviceRegistration> _devices = [];
    private static readonly Dictionary<Guid, NotificationTemplate> _templates = [];

    public Task<Result> SendPushAsync(
        string userId, Guid templateId, Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[STUB] Push notification intent: userId={UserId} templateId={TemplateId} params={@Params}",
            userId, templateId, parameters);

        // No-op — real FCM/APNs dispatch deferred
        return Task.FromResult(Result.Success());
    }

    public Task<Result<DeviceRegistration>> RegisterDeviceAsync(
        Guid tenantId, Guid shopId, string userId,
        DevicePlatform platform, string? fcmToken, string? apnsToken,
        CancellationToken ct = default)
    {
        var registration = new DeviceRegistration(tenantId, shopId, userId, platform, fcmToken, apnsToken);
        _devices[registration.Id] = registration;

        logger.LogInformation(
            "Device registered: userId={UserId} platform={Platform} id={Id}",
            userId, platform, registration.Id);

        return Task.FromResult(Result.Success(registration));
    }

    public Task<Result> UnregisterDeviceAsync(Guid registrationId, CancellationToken ct = default)
    {
        if (!_devices.Remove(registrationId))
        {
            logger.LogWarning("UnregisterDevice: registration {Id} not found.", registrationId);
            return Task.FromResult(Result.Failure($"Device registration {registrationId} not found."));
        }

        logger.LogInformation("Device unregistered: {Id}", registrationId);
        return Task.FromResult(Result.Success());
    }
}

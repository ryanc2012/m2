using M2.SharedKernel;

namespace M2.Domain.Notifications;

public interface INotificationService
{
    Task<Result> SendPushAsync(
        string userId,
        Guid templateId,
        Dictionary<string, string> parameters,
        CancellationToken ct = default);

    Task<Result<DeviceRegistration>> RegisterDeviceAsync(
        Guid tenantId,
        Guid shopId,
        string userId,
        DevicePlatform platform,
        string? fcmToken,
        string? apnsToken,
        CancellationToken ct = default);

    Task<Result> UnregisterDeviceAsync(Guid registrationId, CancellationToken ct = default);
}

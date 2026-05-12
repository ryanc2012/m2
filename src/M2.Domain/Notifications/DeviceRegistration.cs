using M2.SharedKernel;

namespace M2.Domain.Notifications;

public class DeviceRegistration : BaseEntity
{
    public string UserId { get; protected set; } = string.Empty;
    public DevicePlatform Platform { get; protected set; }
    public string? FcmToken { get; protected set; }
    public string? ApnsToken { get; protected set; }
    public DateTimeOffset RegisteredAt { get; protected set; }

    protected DeviceRegistration() { }

    public DeviceRegistration(
        Guid tenantId,
        Guid shopId,
        string userId,
        DevicePlatform platform,
        string? fcmToken,
        string? apnsToken)
    {
        TenantId = tenantId;
        ShopId = shopId;
        UserId = userId;
        Platform = platform;
        FcmToken = fcmToken;
        ApnsToken = apnsToken;
        RegisteredAt = DateTimeOffset.UtcNow;
    }
}

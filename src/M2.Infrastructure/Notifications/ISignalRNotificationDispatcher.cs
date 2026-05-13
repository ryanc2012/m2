namespace M2.Infrastructure.Notifications;

/// <summary>
/// Thin abstraction over SignalR hub context so NotificationService (Infrastructure)
/// can push real-time events without a direct dependency on the Platform.Api assembly.
/// Platform.Api registers <see cref="SignalRNotificationDispatcher"/> against this interface.
/// </summary>
public interface ISignalRNotificationDispatcher
{
    Task SendToUserAsync(string userId, object payload, CancellationToken ct = default);
}

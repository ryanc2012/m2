using M2.Infrastructure.Notifications;
using M2.Platform.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace M2.Platform.Api.Hubs;

/// <summary>
/// Implements <see cref="ISignalRNotificationDispatcher"/> using the in-process
/// <see cref="IHubContext{THub}"/> for <see cref="NotificationHub"/>.
/// </summary>
internal sealed class SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    : ISignalRNotificationDispatcher
{
    public Task SendToUserAsync(string userId, object payload, CancellationToken ct = default)
        => hubContext.Clients.User(userId).SendAsync("ReceiveNotification", payload, ct);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace M2.Platform.Api.Hubs;

/// <summary>
/// SignalR hub for real-time push notifications to Portal / BFF clients.
/// Hub path: /hubs/notifications
/// Auth: X-Api-Key passed as ?access_token= query param (WebSocket transport requirement).
/// The server pushes; clients listen on the "ReceiveNotification" event.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Identity.Web;

namespace M2Portal.Services;

public record NotificationEntry(string Title, DateTimeOffset ReceivedAt)
{
    public bool IsRead { get; set; }
}

public class NotificationHubService(IConfiguration config, ITokenAcquisition tokenAcquisition) : INotificationHubService
{
    private HubConnection? _connection;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        var hubUrl = config["Platform:NotificationHubUrl"] ?? "https://localhost:5100/hubs/notifications";
        var scope = config["AzureAd:PortalBffScope"] ?? "api://m2-portal-bff/.default";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    try { return await tokenAcquisition.GetAccessTokenForUserAsync([scope]); }
                    catch { return null; }
                };
            })
            .WithAutomaticReconnect()
            .Build();

        await _connection.StartAsync(ct);
    }

    public void OnApprovalStateChanged(Func<Guid, string, Task> handler)
    {
        _connection?.On<Guid, string>("ApprovalStateChanged", handler);
    }

    public void OnNotificationReceived(Func<NotificationEntry, Task> handler)
    {
        _connection?.On<string, string>("ReceiveNotification", async (title, _) =>
        {
            await handler(new NotificationEntry(title, DateTimeOffset.UtcNow));
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
    }
}

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Identity.Web;

namespace M2Portal.Services;

public class NotificationHubService(IConfiguration config, ITokenAcquisition tokenAcquisition) : IAsyncDisposable
{
    private HubConnection? _connection;

    public async Task StartAsync(CancellationToken ct = default)
    {
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

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
    }
}

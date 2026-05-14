using M2Portal.Services;

namespace M2Portal.Shared;

public partial class NotificationBell : IAsyncDisposable
{
    private List<NotificationEntry> _notifications = [];
    private int _unreadCount => _notifications.Count(n => !n.IsRead);
    private bool _panelOpen;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await HubService.StartAsync();
            HubService.OnNotificationReceived(async entry =>
            {
                await InvokeAsync(() =>
                {
                    _notifications.Insert(0, entry);
                    StateHasChanged();
                });
            });
        }
        catch
        {
            // SignalR unavailable in dev — degrade gracefully
        }
    }

    private void TogglePanel() => _panelOpen = !_panelOpen;

    private Task MarkAllReadAsync()
    {
        foreach (var n in _notifications) n.IsRead = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await HubService.DisposeAsync();
}

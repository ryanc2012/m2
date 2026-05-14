namespace M2Portal.Services;

public interface INotificationHubService : IAsyncDisposable
{
    Task StartAsync(CancellationToken ct = default);
    void OnApprovalStateChanged(Func<Guid, string, Task> handler);
    void OnNotificationReceived(Func<NotificationEntry, Task> handler);
}

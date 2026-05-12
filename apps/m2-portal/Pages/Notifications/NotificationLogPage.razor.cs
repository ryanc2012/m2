using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Notifications;

public partial class NotificationLogPage
{
    [Inject] private NotificationLogService NotificationLogSvc { get; set; } = null!;

    private List<NotificationLogEntry> _entries = [];
    private bool _loading = true;
    private string? _error;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _memberIdFilter = string.Empty;

    private readonly List<BreadcrumbItem> _breadcrumbs =
    [
        new("Home", href: "/"),
        new("Notification Log", href: null, disabled: true),
    ];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var from  = _fromDate.HasValue  ? DateOnly.FromDateTime(_fromDate.Value)  : (DateOnly?)null;
            var to    = _toDate.HasValue    ? DateOnly.FromDateTime(_toDate.Value)    : (DateOnly?)null;
            var memId = string.IsNullOrWhiteSpace(_memberIdFilter) ? null : _memberIdFilter;

            _entries = [.. await NotificationLogSvc.ListAsync(
                from: from,
                to: to,
                memberId: memId)];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load notifications: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private static Color StatusColor(NotificationStatus status) => status switch
    {
        NotificationStatus.Delivered => Color.Success,
        NotificationStatus.Sent      => Color.Info,
        NotificationStatus.Failed    => Color.Error,
        _                            => Color.Default,
    };
}

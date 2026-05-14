using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Approvals;

public partial class ApprovalList : IAsyncDisposable
{
    [Inject] private ApprovalService ApprovalSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NotificationHubService HubService { get; set; } = null!;

    private List<ApprovalRequest> _approvals = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        await ConnectSignalRAsync();
    }

    private async Task ConnectSignalRAsync()
    {
        try
        {
            await HubService.StartAsync();
            HubService.OnApprovalStateChanged(async (_, _) =>
            {
                await InvokeAsync(async () =>
                {
                    await LoadAsync();
                    StateHasChanged();
                });
            });
        }
        catch
        {
            // SignalR unavailable in dev without real Azure AD — degrade gracefully
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _approvals = [.. await ApprovalSvc.GetPendingAsync()];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load approvals: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task QuickApproveAsync(Guid id)
    {
        try
        {
            await ApprovalSvc.ApproveAsync(id, comment: null);
            Snackbar.Add("Approved successfully.", Severity.Success);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to approve. Please try again.", Severity.Error);
        }
    }

    private async Task QuickRejectAsync(Guid id)
    {
        try
        {
            await ApprovalSvc.RejectAsync(id, comment: null);
            Snackbar.Add("Rejected.", Severity.Warning);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to reject. Please try again.", Severity.Error);
        }
    }

    private static Color StatusColor(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Pending   => Color.Warning,
        ApprovalStatus.Approved  => Color.Success,
        ApprovalStatus.Rejected  => Color.Error,
        ApprovalStatus.Escalated => Color.Info,
        _                        => Color.Default,
    };

    public async ValueTask DisposeAsync() => await HubService.DisposeAsync();
}

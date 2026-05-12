using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Approvals;

public partial class ApprovalDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private ApprovalService ApprovalSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private ApprovalDetailDto? _detail;
    private bool _loading = true;
    private bool _acting = false;
    private string? _error;
    private string _comment = string.Empty;

    private List<BreadcrumbItem> _breadcrumbs =
    [
        new("Approvals", href: "approvals"),
        new("Detail", href: null, disabled: true),
    ];

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _detail = await ApprovalSvc.GetDetailAsync(Id);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load approval: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ApproveAsync()
    {
        _acting = true;
        try
        {
            await ApprovalSvc.ApproveAsync(Id, _comment.NullIfEmpty());
            Snackbar.Add("Approved.", Severity.Success);
            Nav.NavigateTo("approvals");
        }
        catch
        {
            Snackbar.Add("Failed to approve. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
        }
    }

    private async Task RejectAsync()
    {
        _acting = true;
        try
        {
            await ApprovalSvc.RejectAsync(Id, _comment.NullIfEmpty());
            Snackbar.Add("Rejected.", Severity.Warning);
            Nav.NavigateTo("approvals");
        }
        catch
        {
            Snackbar.Add("Failed to reject. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
        }
    }

    private async Task EscalateAsync()
    {
        _acting = true;
        try
        {
            await ApprovalSvc.EscalateAsync(Id, _comment.NullIfEmpty());
            Snackbar.Add("Escalated.", Severity.Info);
            Nav.NavigateTo("approvals");
        }
        catch
        {
            Snackbar.Add("Failed to escalate. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
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

    private static Color StepColor(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Approved  => Color.Success,
        ApprovalStatus.Rejected  => Color.Error,
        ApprovalStatus.Escalated => Color.Info,
        _                        => Color.Warning,
    };
}

internal static class StringExtensions
{
    internal static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}

using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Reporting;

public partial class AttendanceSummaryPage
{
    [Inject] private ReportingService ReportingSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;
    private List<AttendanceSummaryRow> _rows = [];
    private bool _loading;
    private string? _error;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _rows = [.. await ReportingSvc.GetAttendanceSummaryAsync(_fromDate, _toDate)];
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }
}

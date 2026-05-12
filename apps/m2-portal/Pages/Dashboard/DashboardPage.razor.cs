using Microsoft.AspNetCore.Components;
using M2Portal.Services;

namespace M2Portal.Pages.Dashboard;

public partial class DashboardPage
{
    [Inject] private IDashboardService DashboardSvc { get; set; } = null!;

    private DashboardSummary? _summary;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _summary = await DashboardSvc.GetSummaryAsync();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }
}

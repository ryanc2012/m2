using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Promotions;

public partial class PromotionList
{
    [Inject] private PromotionService PromotionSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<PromotionSummary> _promotions = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _promotions = [.. await PromotionSvc.GetAllAsync()];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load promotions: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private static Color StatusColor(PromotionStatus status) => status switch
    {
        PromotionStatus.Active          => Color.Success,
        PromotionStatus.Draft           => Color.Default,
        PromotionStatus.PendingApproval => Color.Warning,
        PromotionStatus.Paused          => Color.Info,
        PromotionStatus.Expired         => Color.Error,
        _                               => Color.Default,
    };
}

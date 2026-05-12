using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Promotions;

public partial class PromotionDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private PromotionService PromotionSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private PromotionDetailDto? _detail;
    private bool _loading = true;
    private bool _acting;
    private string? _error;

    private List<BreadcrumbItem> _breadcrumbs =
    [
        new("Promotions", href: "promotions"),
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
            _detail = await PromotionSvc.GetDetailAsync(Id);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load promotion: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ActivateAsync()
    {
        _acting = true;
        try
        {
            await PromotionSvc.ActivateAsync(Id);
            Snackbar.Add("Promotion activated.", Severity.Success);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to activate. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
        }
    }

    private async Task PauseAsync()
    {
        _acting = true;
        try
        {
            await PromotionSvc.PauseAsync(Id);
            Snackbar.Add("Promotion paused.", Severity.Warning);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to pause. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
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

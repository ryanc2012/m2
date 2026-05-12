using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.GoodsReceipt;

public partial class GoodsReceiptDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private GoodsReceiptService GoodsReceiptSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private M2Portal.Services.GoodsReceiptDetail? _detail;
    private bool _loading = true;
    private bool _acting;
    private string? _error;
    private bool _dialogOpen;
    private string _discrepancyNote = string.Empty;

    private List<BreadcrumbItem> _breadcrumbs =>
    [
        new("Home", href: "/"),
        new("Goods Receipt", href: "/goods-receipt"),
        new(_detail?.SapDeliveryNote ?? Id.ToString(), href: null, disabled: true),
    ];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _detail = await GoodsReceiptSvc.GetAsync(Id);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load receipt: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ConfirmReceiptAsync()
    {
        _acting = true;
        try
        {
            await GoodsReceiptSvc.ConfirmAsync(Id);
            Snackbar.Add("Receipt confirmed successfully.", Severity.Success);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to confirm. Please try again.", Severity.Error);
        }
        finally
        {
            _acting = false;
        }
    }

    private async Task SubmitDiscrepancyAsync()
    {
        if (string.IsNullOrWhiteSpace(_discrepancyNote)) return;
        _dialogOpen = false;
        _acting = true;
        try
        {
            await GoodsReceiptSvc.RecordDiscrepancyAsync(Id, _discrepancyNote);
            Snackbar.Add("Discrepancy recorded.", Severity.Warning);
            _discrepancyNote = string.Empty;
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to record discrepancy.", Severity.Error);
        }
        finally
        {
            _acting = false;
        }
    }

    private static Color StatusColor(GoodsReceiptStatus status) => status switch
    {
        GoodsReceiptStatus.Pending      => Color.Warning,
        GoodsReceiptStatus.Confirmed    => Color.Success,
        GoodsReceiptStatus.Discrepancy  => Color.Error,
        _                               => Color.Default,
    };

    private static string StatusLabel(GoodsReceiptStatus status) => status switch
    {
        GoodsReceiptStatus.Pending      => "Pending",
        GoodsReceiptStatus.Confirmed    => "Confirmed",
        GoodsReceiptStatus.Discrepancy  => "Discrepancy",
        _                               => status.ToString(),
    };
}

using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.GoodsReceipt;

public partial class GoodsReceiptList
{
    [Inject] private GoodsReceiptService GoodsReceiptSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<GoodsReceiptSummary> _receipts = [];
    private bool _loading = true;
    private string? _error;

    // Demo tenant/shop IDs — in production these come from the authenticated user's claims.
    private static readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _shopId   = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly List<BreadcrumbItem> _breadcrumbs =
    [
        new("Home", href: "/"),
        new("Goods Receipt", href: null, disabled: true),
    ];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _receipts = [.. await GoodsReceiptSvc.ListAsync(_tenantId, _shopId)];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load goods receipts: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ConfirmAsync(Guid id)
    {
        try
        {
            await GoodsReceiptSvc.ConfirmAsync(id);
            Snackbar.Add("Goods receipt confirmed.", Severity.Success);
            await LoadAsync();
        }
        catch
        {
            Snackbar.Add("Failed to confirm. Please try again.", Severity.Error);
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

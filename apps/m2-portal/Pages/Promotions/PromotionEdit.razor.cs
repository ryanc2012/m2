using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Promotions;

public partial class PromotionEdit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private PromotionService PromotionSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private MudForm _form = null!;

    private PromotionDetailDto? _detail;
    private bool _loading = true;
    private bool _submitting;
    private string? _error;

    // Editable fields — populated from _detail on load
    private string _nameEn = string.Empty;
    private string _nameZht = string.Empty;
    private string _formulaJson = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _isStackable;

    private List<BreadcrumbItem> _breadcrumbs =
    [
        new("Promotions", href: "promotions"),
        new("Edit", href: null, disabled: true),
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
            _nameEn = _detail.NameEn;
            _nameZht = _detail.NameZht;
            _formulaJson = _detail.FormulaJson;
            _startDate = _detail.StartDate.ToDateTime(TimeOnly.MinValue);
            _endDate = _detail.EndDate.ToDateTime(TimeOnly.MinValue);
            _isStackable = _detail.IsStackable;
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

    private async Task SaveAsync()
    {
        await _form.Validate();
        if (!_form.IsValid) return;
        if (_startDate is null || _endDate is null)
        {
            Snackbar.Add("Start and end dates are required.", Severity.Warning);
            return;
        }

        _submitting = true;
        try
        {
            var request = new UpdatePromotionRequest(
                NameEn: _nameEn.Trim(),
                NameZht: _nameZht.Trim(),
                FormulaJson: _formulaJson.Trim(),
                StartDate: DateOnly.FromDateTime(_startDate.Value),
                EndDate: DateOnly.FromDateTime(_endDate.Value),
                IsStackable: _isStackable);

            await PromotionSvc.UpdateAsync(Id, request);
            Snackbar.Add("Promotion updated.", Severity.Success);
            Nav.NavigateTo($"promotions/{Id}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save: {ex.Message}", Severity.Error);
        }
        finally
        {
            _submitting = false;
        }
    }
}

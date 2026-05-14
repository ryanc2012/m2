using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Promotions;

public partial class PromotionCreate
{
    [Inject] private PromotionService PromotionSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private MudForm _form = null!;

    private string _nameEn = string.Empty;
    private string _nameZht = string.Empty;
    private PromotionType _type = PromotionType.Percentage;
    private string _formulaJson = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _isStackable;
    private bool _submitting;

    private List<BreadcrumbItem> _breadcrumbs =
    [
        new("Promotions", href: "promotions"),
        new("New", href: null, disabled: true),
    ];

    private async Task SubmitAsync()
    {
        await _form.Validate();
        if (!_form.IsValid) return;
        if (_startDate is null || _endDate is null)
        {
            Snackbar.Add("Start and end dates are required.", Severity.Warning);
            return;
        }
        if (_endDate <= _startDate)
        {
            Snackbar.Add("End date must be after start date.", Severity.Warning);
            return;
        }

        _submitting = true;
        try
        {
            var request = new CreatePromotionRequest(
                NameEn: _nameEn.Trim(),
                NameZht: _nameZht.Trim(),
                Type: _type,
                FormulaJson: _formulaJson.Trim(),
                StartDate: DateOnly.FromDateTime(_startDate.Value),
                EndDate: DateOnly.FromDateTime(_endDate.Value),
                IsStackable: _isStackable);

            var created = await PromotionSvc.CreateAsync(request);
            Snackbar.Add("Promotion created successfully.", Severity.Success);
            Nav.NavigateTo($"promotions/{created.Id}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create promotion: {ex.Message}", Severity.Error);
        }
        finally
        {
            _submitting = false;
        }
    }
}

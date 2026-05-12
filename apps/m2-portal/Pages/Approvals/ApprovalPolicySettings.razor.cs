using Microsoft.AspNetCore.Components;
using M2Portal.Services;
using MudBlazor;

namespace M2Portal.Pages.Approvals;

public partial class ApprovalPolicySettings
{
    [Inject] private ApprovalService ApprovalSvc { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<ApprovalPolicyConfig> _policies = [];
    private bool _loading = true;
    private bool _saving = false;
    private string? _error;

    // Inline-edit state
    private string? _editingEntityType;
    private EscalationMode _editMode;
    private int _editMaxLevels;

    private List<BreadcrumbItem> _breadcrumbs =
    [
        new("Approvals", href: "approvals"),
        new("Policy Settings", href: null, disabled: true),
    ];

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
            _policies = [.. await ApprovalSvc.GetPoliciesAsync()];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load policies: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void BeginEdit(ApprovalPolicyConfig config)
    {
        _editingEntityType = config.EntityType;
        _editMode = config.Mode;
        _editMaxLevels = config.MaxLevels;
    }

    private void CancelEdit()
    {
        _editingEntityType = null;
    }

    private async Task SavePolicyAsync(string entityType)
    {
        _saving = true;
        try
        {
            var config = new ApprovalPolicyConfig(entityType, _editMode, _editMaxLevels);
            await ApprovalSvc.SavePolicyAsync(config);

            // Update in-memory list
            var idx = _policies.FindIndex(p => p.EntityType == entityType);
            if (idx >= 0) _policies[idx] = config;

            _editingEntityType = null;
            Snackbar.Add($"Policy for '{entityType}' saved.", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Failed to save policy. Please try again.", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private static string ModeLabel(EscalationMode mode) => mode switch
    {
        EscalationMode.SapHcmHierarchy     => "SAP HCM Hierarchy",
        EscalationMode.StepByStepPosition  => "Step-by-step Position",
        _                                  => mode.ToString(),
    };
}

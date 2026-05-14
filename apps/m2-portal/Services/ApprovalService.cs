using System.Net.Http.Json;

namespace M2Portal.Services;

public enum ApprovalStatus { Pending, Approved, Rejected, Escalated }

public enum EscalationMode { SapHcmHierarchy, StepByStepPosition }

public record ApprovalRequest(
    Guid Id,
    string EntityType,
    string EntityId,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    ApprovalStatus Status);

public record ApprovalStep(
    int StepIndex,
    string Actor,
    ApprovalStatus Status,
    string? Comment,
    DateTimeOffset? ActionAt);

public record ApprovalDetailDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    ApprovalStatus Status,
    int CurrentStep,
    IReadOnlyList<ApprovalStep> History);

public record ApprovalPolicyConfig(
    string EntityType,
    EscalationMode Mode,
    int MaxLevels);

/// <summary>
/// Stub service — calls M2PortalBff /approvals/* endpoints.
/// All methods will throw HttpRequestException when the BFF is unavailable (Sprint 2 stub).
/// </summary>
public class ApprovalService(HttpClient http)
{
    /// GET /api/v1/approvals?status=pending
    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingAsync(
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApprovalRequest>>(
            "/api/v1/approvals?status=pending", ct) ?? [];
    }

    /// GET /api/v1/approvals/{id}
    public async Task<ApprovalDetailDto> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<ApprovalDetailDto>($"/api/v1/approvals/{id}", ct)
            ?? throw new KeyNotFoundException($"Approval {id} not found.");
    }

    /// POST /api/v1/approvals/{id}/approve
    public async Task ApproveAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/api/v1/approvals/{id}/approve", new { comment }, ct);
    }

    /// POST /api/v1/approvals/{id}/reject
    public async Task RejectAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/api/v1/approvals/{id}/reject", new { comment }, ct);
    }

    /// POST /api/v1/approvals/{id}/escalate
    public async Task EscalateAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/api/v1/approvals/{id}/escalate", new { comment }, ct);
    }

    /// GET /api/v1/approvals/policies
    public async Task<IReadOnlyList<ApprovalPolicyConfig>> GetPoliciesAsync(
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApprovalPolicyConfig>>(
            "/api/v1/approvals/policies", ct) ?? [];
    }

    /// PUT /api/v1/approvals/policies/{entityType}
    public async Task SavePolicyAsync(ApprovalPolicyConfig config, CancellationToken ct = default)
    {
        await http.PutAsJsonAsync($"/api/v1/approvals/policies/{config.EntityType}", config, ct);
    }
}

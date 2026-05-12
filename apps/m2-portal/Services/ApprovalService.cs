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
    /// GET /approvals?status=pending
    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingAsync(
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApprovalRequest>>(
            "/approvals?status=pending", ct) ?? [];
    }

    /// GET /approvals/{id}
    public async Task<ApprovalDetailDto> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<ApprovalDetailDto>($"/approvals/{id}", ct)
            ?? throw new KeyNotFoundException($"Approval {id} not found.");
    }

    /// POST /approvals/{id}/approve
    public async Task ApproveAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/approvals/{id}/approve", new { comment }, ct);
    }

    /// POST /approvals/{id}/reject
    public async Task RejectAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/approvals/{id}/reject", new { comment }, ct);
    }

    /// POST /approvals/{id}/escalate
    public async Task EscalateAsync(Guid id, string? comment, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync($"/approvals/{id}/escalate", new { comment }, ct);
    }

    /// GET /approvals/policies
    public async Task<IReadOnlyList<ApprovalPolicyConfig>> GetPoliciesAsync(
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApprovalPolicyConfig>>(
            "/approvals/policies", ct) ?? [];
    }

    /// PUT /approvals/policies/{entityType}
    public async Task SavePolicyAsync(ApprovalPolicyConfig config, CancellationToken ct = default)
    {
        await http.PutAsJsonAsync($"/approvals/policies/{config.EntityType}", config, ct);
    }
}

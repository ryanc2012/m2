using System.Net.Http.Json;

namespace M2Portal.Services;

public enum PromotionType { Percentage, Fixed, BuyXGetY }

public enum PromotionStatus { Draft, PendingApproval, Active, Paused, Expired }

public record PromotionSummary(
    Guid Id,
    string NameEn,
    string NameZht,
    PromotionType Type,
    PromotionStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsStackable);

public record PromotionDetailDto(
    Guid Id,
    string NameEn,
    string NameZht,
    PromotionType Type,
    PromotionStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsStackable,
    string FormulaJson,
    Guid? ApprovalRequestId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreatePromotionRequest(
    string NameEn,
    string NameZht,
    PromotionType Type,
    string FormulaJson,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsStackable);

public record UpdatePromotionRequest(
    string NameEn,
    string NameZht,
    string FormulaJson,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsStackable);

/// <summary>
/// Stub service — calls M2PortalBff /promotions/* endpoints.
/// </summary>
public class PromotionService(HttpClient http)
{
    /// GET /api/v1/promotions
    public async Task<IReadOnlyList<PromotionSummary>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<PromotionSummary>>(
            "/api/v1/promotions", ct) ?? [];
    }

    /// GET /api/v1/promotions/{id}
    public async Task<PromotionDetailDto> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<PromotionDetailDto>($"/api/v1/promotions/{id}", ct)
            ?? throw new KeyNotFoundException($"Promotion {id} not found.");
    }

    /// POST /api/v1/promotions
    public async Task<PromotionDetailDto> CreateAsync(
        CreatePromotionRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/v1/promotions", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PromotionDetailDto>(ct)
            ?? throw new InvalidOperationException("Empty response from server.");
    }

    /// PUT /api/v1/promotions/{id}
    public async Task UpdateAsync(
        Guid id, UpdatePromotionRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"/api/v1/promotions/{id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    /// POST /api/v1/promotions/{id}/activate
    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"/api/v1/promotions/{id}/activate", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// POST /api/v1/promotions/{id}/pause
    public async Task PauseAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"/api/v1/promotions/{id}/pause", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// POST /api/v1/promotions/{id}/submit
    public async Task SubmitForApprovalAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"/api/v1/promotions/{id}/submit", null, ct);
        response.EnsureSuccessStatusCode();
    }
}

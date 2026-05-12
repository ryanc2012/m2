using System.Net.Http.Json;

namespace M2Portal.Services;

public enum GoodsReceiptStatus { Pending, Confirmed, Discrepancy }

public record GoodsReceiptLine(
    int LineNumber,
    string ProductCode,
    string ProductNameZht,
    int ExpectedQty,
    int ReceivedQty,
    string? DiscrepancyNote)
{
    public int Discrepancy => ReceivedQty - ExpectedQty;
}

public record GoodsReceiptSummary(
    Guid Id,
    string SapDeliveryNote,
    GoodsReceiptStatus Status,
    DateTimeOffset ReceivedDate);

public record GoodsReceiptDetail(
    Guid Id,
    string SapDeliveryNote,
    GoodsReceiptStatus Status,
    DateTimeOffset ReceivedDate,
    IReadOnlyList<GoodsReceiptLine> Lines);

/// <summary>
/// Typed HTTP client wrapping M2PortalBff /api/goods-receipt/ endpoints.
/// Returns mock data when the BFF is unavailable (Sprint 4 stub).
/// </summary>
public class GoodsReceiptService(HttpClient http)
{
    /// GET /api/goods-receipt/?tenantId={tenantId}&shopId={shopId}
    public async Task<IReadOnlyList<GoodsReceiptSummary>> ListAsync(
        Guid tenantId, Guid shopId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<List<GoodsReceiptSummary>>(
                $"/api/goods-receipt/?tenantId={tenantId}&shopId={shopId}", ct) ?? [];
        }
        catch
        {
            return MockSummaries();
        }
    }

    /// GET /api/goods-receipt/{id}
    public async Task<GoodsReceiptDetail> GetAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<GoodsReceiptDetail>(
                $"/api/goods-receipt/{id}", ct)
                ?? throw new KeyNotFoundException($"Goods receipt {id} not found.");
        }
        catch
        {
            return MockDetail(id);
        }
    }

    /// POST /api/goods-receipt/{id}/confirm
    public async Task ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"/api/goods-receipt/{id}/confirm", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// POST /api/goods-receipt/{id}/discrepancy
    public async Task RecordDiscrepancyAsync(Guid id, string note, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            $"/api/goods-receipt/{id}/discrepancy", new { note }, ct);
        response.EnsureSuccessStatusCode();
    }

    private static IReadOnlyList<GoodsReceiptSummary> MockSummaries() =>
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "4900012345", GoodsReceiptStatus.Pending,
            DateTimeOffset.Now.AddHours(-2)),
        new(Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "4900012346", GoodsReceiptStatus.Confirmed,
            DateTimeOffset.Now.AddDays(-1)),
        new(Guid.Parse("00000000-0000-0000-0000-000000000003"),
            "4900012347", GoodsReceiptStatus.Discrepancy,
            DateTimeOffset.Now.AddDays(-2)),
    ];

    private static GoodsReceiptDetail MockDetail(Guid id) => new(
        id,
        "4900012345",
        GoodsReceiptStatus.Pending,
        DateTimeOffset.Now.AddHours(-2),
        [
            new(1, "PRD-001", "洗髮精 500ml", 24, 24, null),
            new(2, "PRD-002", "沐浴乳 300ml", 12, 10, "2件破損"),
        ]);
}

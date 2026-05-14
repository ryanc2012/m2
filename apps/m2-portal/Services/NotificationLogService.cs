using System.Net.Http.Json;

namespace M2Portal.Services;

public enum NotificationStatus { Sent, Delivered, Failed }

public record NotificationLogEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    string Type,
    string Recipient,
    NotificationStatus Status,
    string MessagePreview);

/// <summary>
/// Typed HTTP client for notification history log. Falls back to mock data.
/// BFF endpoint: /api/notification-history/
/// </summary>
public class NotificationLogService(HttpClient http)
{
    /// GET /api/notification-history/?shopId={shopId}&from={from}&to={to}&memberId={memberId}
    public async Task<IReadOnlyList<NotificationLogEntry>> ListAsync(
        Guid? shopId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        string? memberId = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = BuildQuery(shopId, from, to, memberId);
            return await http.GetFromJsonAsync<List<NotificationLogEntry>>(
                $"/api/v1/notification-history/{query}", ct) ?? [];
        }
        catch
        {
            return MockEntries();
        }
    }

    private static string BuildQuery(Guid? shopId, DateOnly? from, DateOnly? to, string? memberId)
    {
        var parts = new List<string>();
        if (shopId.HasValue) parts.Add($"shopId={shopId}");
        if (from.HasValue) parts.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) parts.Add($"to={to:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(memberId)) parts.Add($"memberId={memberId}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static IReadOnlyList<NotificationLogEntry> MockEntries() =>
    [
        new(Guid.NewGuid(), DateTimeOffset.Now.AddMinutes(-30),
            "Promotion", "M001234", NotificationStatus.Delivered,
            "新優惠活動開始！夏日護膚優惠現已開始..."),
        new(Guid.NewGuid(), DateTimeOffset.Now.AddHours(-2),
            "Coupon", "M005678", NotificationStatus.Delivered,
            "優惠券即將到期，請於明天前使用..."),
        new(Guid.NewGuid(), DateTimeOffset.Now.AddHours(-5),
            "System", "M009999", NotificationStatus.Failed,
            "帳戶積分更新通知..."),
        new(Guid.NewGuid(), DateTimeOffset.Now.AddDays(-1),
            "Promotion", "M002345", NotificationStatus.Sent,
            "會員專屬優惠：消費滿 RM100 享 10% 折扣..."),
    ];
}

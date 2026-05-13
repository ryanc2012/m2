using System.Net.Http.Json;
using M2.Domain.Notifications.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Notifications module's /modules/notifications/* endpoints.
/// Adds X-Internal-Call and X-Internal-Secret headers on every request.
/// </summary>
public sealed class NotificationsModuleClient : INotificationsModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public NotificationsModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result> SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "send", request);
            var response = await _httpClient.SendAsync(req, ct);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure($"Notifications module returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Inter-module call failed: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<NotificationLogDto>>> GetHistoryByMemberAsync(
        Guid tenantId, string memberId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var url = $"history/member/{Uri.EscapeDataString(memberId)}?tenantId={tenantId}&page={page}&pageSize={pageSize}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<PagedResult<NotificationLogDto>>(
                    $"Notifications module returned {(int)response.StatusCode}");

            var result = await response.Content.ReadFromJsonAsync<PagedResult<NotificationLogDto>>(ct);
            return result is not null
                ? Result.Success(result)
                : Result.Failure<PagedResult<NotificationLogDto>>("Empty response from notifications module");
        }
        catch (Exception ex)
        {
            return Result.Failure<PagedResult<NotificationLogDto>>($"Inter-module call failed: {ex.Message}");
        }
    }

    public async Task<Result> MarkReadAsync(Guid notificationLogId, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Patch, $"history/{notificationLogId}/read");
            var response = await _httpClient.SendAsync(req, ct);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure($"Notifications module returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Inter-module call failed: {ex.Message}");
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl, object? body = null)
    {
        var msg = new HttpRequestMessage(method, relativeUrl);
        msg.Headers.Add(InternalCallHeader, "true");
        msg.Headers.Add(InternalSecretHeader, _secret);
        if (body is not null)
            msg.Content = JsonContent.Create(body);
        return msg;
    }
}

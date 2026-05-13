using System.Net.Http.Json;
using M2.Domain.Reporting;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Reporting module's /modules/reporting/* endpoints.
/// </summary>
public sealed class ReportingModuleClient : IReportingModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public ReportingModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<SalesSummary>> GetDailySalesSummaryAsync(
        Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default)
    {
        try
        {
            var url = $"sales/daily?tenantId={tenantId}&shopId={shopId}&date={date:O}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<SalesSummary>(response);
        }
        catch (Exception ex) { return Result.Failure<SalesSummary>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<AttendanceSummary>> GetAttendanceSummaryAsync(
        Guid tenantId, Guid shopId, DateOnly date, CancellationToken ct = default)
    {
        try
        {
            var url = $"attendance/daily?tenantId={tenantId}&shopId={shopId}&date={date:O}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<AttendanceSummary>(response);
        }
        catch (Exception ex) { return Result.Failure<AttendanceSummary>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<PromotionPerformance>> GetPromotionPerformanceAsync(
        Guid tenantId, Guid shopId, Guid promotionId, CancellationToken ct = default)
    {
        try
        {
            var url = $"promotions/{promotionId}/performance?tenantId={tenantId}&shopId={shopId}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<PromotionPerformance>(response);
        }
        catch (Exception ex) { return Result.Failure<PromotionPerformance>($"Inter-module call failed: {ex.Message}"); }
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

    private static async Task<Result<T>> ReadDto<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return Result.Failure<T>($"Reporting module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from reporting module");
    }
}

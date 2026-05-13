using System.Net.Http.Json;
using M2.Domain.Attendance;
using M2.Domain.Attendance.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Attendance module's /modules/attendance/* endpoints.
/// </summary>
public sealed class AttendanceModuleClient : IAttendanceModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public AttendanceModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<AttendanceRecordDto>> ClockInAsync(ClockInPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "clock-in", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<AttendanceRecordDto>(response);
        }
        catch (Exception ex) { return Result.Failure<AttendanceRecordDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<AttendanceRecordDto>> ClockOutAsync(ClockOutPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "clock-out", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<AttendanceRecordDto>(response);
        }
        catch (Exception ex) { return Result.Failure<AttendanceRecordDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<IReadOnlyList<AttendanceRecordDto>>> GetRecordsForEmployeeAsync(
        Guid tenantId,
        string employeeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"records?tenantId={tenantId}&employeeId={Uri.EscapeDataString(employeeId)}";
            if (from.HasValue) url += $"&from={Uri.EscapeDataString(from.Value.ToString("O"))}";
            if (to.HasValue) url += $"&to={Uri.EscapeDataString(to.Value.ToString("O"))}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<AttendanceRecordDto>>($"Attendance module returned {(int)response.StatusCode}");
            var list = await response.Content.ReadFromJsonAsync<List<AttendanceRecordDto>>();
            return list is not null
                ? Result.Success<IReadOnlyList<AttendanceRecordDto>>(list.AsReadOnly())
                : Result.Failure<IReadOnlyList<AttendanceRecordDto>>("Empty response from attendance module");
        }
        catch (Exception ex) { return Result.Failure<IReadOnlyList<AttendanceRecordDto>>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<AttendanceSummary>> GetDailySummaryAsync(
        Guid tenantId,
        string employeeId,
        DateOnly date,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"summary?tenantId={tenantId}&employeeId={Uri.EscapeDataString(employeeId)}&date={date:O}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<AttendanceSummary>(response);
        }
        catch (Exception ex) { return Result.Failure<AttendanceSummary>($"Inter-module call failed: {ex.Message}"); }
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
            return Result.Failure<T>($"Attendance module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from attendance module");
    }
}
